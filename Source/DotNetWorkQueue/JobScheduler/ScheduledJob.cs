//Copyright(c) 2015 Bret Copeland<bret@atlantisflight.org>
//
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of
//this software and associated documentation files (the "Software"), to deal in 
//the Software without restriction, including without limitation the rights to
//use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
//the Software, and to permit persons to whom the Software is furnished to do so, 
//subject to the following conditions:
// 
//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
//FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR
//COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
//IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
//CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.JobScheduler
{
    internal class ScheduledJob: IScheduledJob
    {
        private readonly object _scheduleLock = new object();
        private int _runId = 0;
        private int _execLocked = 0;
        private readonly JobScheduler _scheduler;
        private readonly IProducerMethodJobQueue _queue;

        public string Name { get; }
        public IJobSchedule Schedule { get; private set; }
        public bool IsScheduleRunning { get; internal set; }
        public bool IsCallbackExecuting => _execLocked == 1;
        public bool IsAttached { get; set; }
        public TimeSpan Window { get; set; }
        public DateTimeOffset NextEvent { get; private set; }
        public DateTimeOffset PrevEvent { get; private set; }
        public ILog Logger => _queue.Logger;

        public event Action<IScheduledJob, Exception> OnException;
        public event Action<IScheduledJob, IJobQueueOutputMessage> OnNonFatalFailureEnQueue;
        public event Action<IScheduledJob, IJobQueueOutputMessage> OnEnQueue;

        private readonly LinqExpressionToRun _expressionToRun;
        private readonly Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> _actionToRun;
        private readonly IGetTime _getTime;

        internal ScheduledJob(JobScheduler scheduler,
            string name,
            IJobSchedule schedule,
            IProducerMethodJobQueue queue,
            LinqExpressionToRun expressionToRun,
            IGetTime time
           )
        {
            _scheduler = scheduler;
            Name = name;
            Schedule = schedule;
            _queue = queue;
            _expressionToRun = expressionToRun;
            _getTime = time;
        }
        internal ScheduledJob(JobScheduler scheduler,
            string name,
            IJobSchedule schedule,
            IProducerMethodJobQueue queue,
            Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> actionToRun,
            IGetTime time)
        {
            _scheduler = scheduler;
            Name = name;
            Schedule = schedule;
            _queue = queue;
            _actionToRun = actionToRun;
            _getTime = time;
        }

        public void StartSchedule()
        {
            lock (_scheduleLock)
            {
                if (!IsAttached)
                    throw new JobSchedulerException("Cannot start task which is not attached to a scheduler.");

                if (IsScheduleRunning)
                    return;

                var firstEvent = default(DateTimeOffset);
                var firstEventSet = false;
                var window = Window;
                var lastKnownEvent = _queue.LastKnownEvent.Get(Name);
                if (window > TimeSpan.Zero && lastKnownEvent != default(DateTimeOffset))
                {
                    // check if we actually want to run the first event right away
                    var prev = Schedule.Previous();
                    lastKnownEvent = lastKnownEvent.AddSeconds(1); // add a second for good measure
                    if (prev > lastKnownEvent && prev > (new DateTimeOffset(_getTime.GetCurrentUtcDate()) - window))
                    {
                        firstEvent = prev;
                        firstEventSet = true;
                    }
                }

                if (!firstEventSet)
                    firstEvent = Schedule.Next();

                while (firstEvent <= PrevEvent)
                {
                    // we don't want to run the same event twice
                    firstEvent = Schedule.Next(firstEvent);
                }

                NextEvent = firstEvent;
                IsScheduleRunning = true;
                QueueNextEvent();
            }
        }

        public void StopSchedule()
        {
            lock (_scheduleLock)
            {
                if (!IsScheduleRunning)
                    return;

                _runId++;
                IsScheduleRunning = false;
            }
        }

        public void UpdateSchedule(string schedule)
        {
            if (Schedule.OriginalText == schedule)
                return;

            UpdateSchedule(new JobSchedule(schedule, () => new DateTimeOffset(_getTime.GetCurrentUtcDate())));
        }

        public void UpdateSchedule(IJobSchedule schedule)
        {
            Guard.NotNull(() => schedule, schedule);
            lock (_scheduleLock)
            {
                var wasRunning = IsScheduleRunning;
                if (wasRunning)
                    StopSchedule();

                Schedule = schedule;

                if (wasRunning)
                    StartSchedule();
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Name;
        }

        internal async Task RunPendingEvent(PendingEvent ev)
        {
            var eventTime = ev.ScheduledTime;
            var execLockTaken = false;
            try
            {
                lock (_scheduleLock)
                {
                    if (ev.RunId != _runId)
                        return;

                    // take execution lock
                    execLockTaken = Interlocked.CompareExchange(ref _execLocked, 1, 0) == 0;
                    if (execLockTaken)
                        PrevEvent = eventTime; // set this here while we're still in the schedule lock
                }

                if (execLockTaken)
                {
                    try
                    {
                        var result = _expressionToRun != null ? await _queue.SendAsync(this, eventTime, _expressionToRun).ConfigureAwait(false) : await _queue.SendAsync(this, eventTime, _actionToRun).ConfigureAwait(false);
                        if (result.Status == JobQueuedStatus.Success || result.Status == JobQueuedStatus.RequeuedDueToErrorStatus)
                        {
                            //_queue.LastKnownEvent.Set(Name, PrevEvent);
                            RaiseEnQueue(result);
                            _queue.Logger.Log(LogLevel.Debug, () => $"job {this} has been queued");
                        }
                        else if (result.Status == JobQueuedStatus.AlreadyQueuedWaiting ||
                                 result.Status == JobQueuedStatus.AlreadyQueuedProcessing)
                        {
                            _queue.Logger.Log(LogLevel.Warn, () => $"Failed to enqueue job {this}, the status is {result.Status}");
                            RaiseNonFatalFailtureEnQueue(result);
                        }
                        else if (result.SendingException != null)
                        {
                            _queue.Logger.ErrorException($"An error has occurred adding job {this} into the queue", result.SendingException);
                            RaiseException(result.SendingException);
                        }
                    }
                    catch (Exception ex)
                    {
                        _queue.Logger.ErrorException($"A fatal error has occurred trying to add job {this} into the queue", ex);
                        RaiseException(ex);
                    }
                }
            }
            finally
            {
                if (execLockTaken)
                    _execLocked = 0; // release exec lock
            }

            // figure out the next time to run the schedule
            lock (_scheduleLock)
            {
                if (ev.RunId != _runId)
                    return;

                try
                {
                    var next = Schedule.Next();
                    if (next <= eventTime)
                        next = Schedule.Next(eventTime);

                    NextEvent = next;
                    QueueNextEvent();
                }
                catch (Exception ex)
                {
                    _runId++;
                    IsScheduleRunning = false;
                    RaiseException(new DotNetWorkQueueException("Schedule has been terminated because the next valid time could not be found.", ex));
                }
            }
        }

        private void QueueNextEvent()
        {
            _scheduler.AddPendingEvent(new PendingEvent(NextEvent, this, _runId));
        }

        private void RaiseException(Exception ex)
        {
            Task.Run(() =>
            {
                var ev = OnException;
                ev?.Invoke(this, ex);
            });
        }
        private void RaiseNonFatalFailtureEnQueue(IJobQueueOutputMessage message)
        {
            Task.Run(() =>
            {
                var ev = OnNonFatalFailureEnQueue;
                ev?.Invoke(this, message);
            });
        }
        private void RaiseEnQueue(IJobQueueOutputMessage message)
        {
            Task.Run(() =>
            {
                var ev = OnEnQueue;
                ev?.Invoke(this, message);
            });
        }
    }
}
