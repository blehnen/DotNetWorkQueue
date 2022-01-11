﻿// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.JobScheduler
{
    /// <summary>
    /// Represents a job that has been scheduled.
    /// </summary>
    /// <seealso cref="IScheduledJob" />
    internal class ScheduledJob: IScheduledJob
    {
        private readonly object _scheduleLock = new object();
        private int _runId;
        private int _execLocked;
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
        public ILogger Logger => _queue.Logger;
        public string Route { get; }
        public bool RawExpression { get; }

        public event Action<IScheduledJob, Exception> OnException;
        public event Action<IScheduledJob, IJobQueueOutputMessage> OnNonFatalFailureEnQueue;
        public event Action<IScheduledJob, IJobQueueOutputMessage> OnEnQueue;

#if NETFULL
        private readonly LinqExpressionToRun _expressionToRun;
#endif
        private readonly Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> _actionToRun;
        private readonly IGetTime _getTime;

        internal ScheduledJob(JobScheduler scheduler,
            string name,
            IJobSchedule schedule,
            IProducerMethodJobQueue queue,
            LinqExpressionToRun expressionToRun,
            IGetTime time,
            string route
           )
        {
            _scheduler = scheduler;
            Name = name;
            Schedule = schedule;
            _queue = queue;

#if NETFULL
            _expressionToRun = expressionToRun;
#endif
            _getTime = time;
            Route = route;
        }
        internal ScheduledJob(JobScheduler scheduler,
            string name,
            IJobSchedule schedule,
            IProducerMethodJobQueue queue,
            Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> actionToRun,
            IGetTime time,
            string route,
            bool rawExpression)
        {
            _scheduler = scheduler;
            Name = name;
            Schedule = schedule;
            _queue = queue;
            _actionToRun = actionToRun;
            _getTime = time;
            Route = route;
            RawExpression = rawExpression;
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
                if (window > TimeSpan.Zero && lastKnownEvent != default)
                {
                    // check if we actually want to run the first event right away
                    var prev = Schedule.Previous();
                    lastKnownEvent = lastKnownEvent.AddSeconds(1); // add a second for good measure
                    if (prev > lastKnownEvent && prev > new DateTimeOffset(_getTime.GetCurrentUtcDate()) - window)
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

        internal async Task RunPendingEventAsync(PendingEvent ev)
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
#if NETFULL
                        var result = _expressionToRun != null ? await _queue.SendAsync(this, eventTime, _expressionToRun).ConfigureAwait(false) : await _queue.SendAsync(this, eventTime, _actionToRun, RawExpression).ConfigureAwait(false);
#else
                        var result = await _queue.SendAsync(this, eventTime, _actionToRun, RawExpression).ConfigureAwait(false);
#endif
                        if (result.Status == JobQueuedStatus.Success || result.Status == JobQueuedStatus.RequeuedDueToErrorStatus)
                        {
                            RaiseEnQueue(result);
                            _queue.Logger.LogDebug($"job {this} queued");
                        }
                        else if (result.Status == JobQueuedStatus.AlreadyQueuedWaiting ||
                                 result.Status == JobQueuedStatus.AlreadyQueuedProcessing ||
                                 result.Status == JobQueuedStatus.AlreadyProcessed)
                        {
                            _queue.Logger.LogWarning( $"Failed to enqueue job {this}, the status is {result.Status}");
                            RaiseNonFatalFailureEnQueue(result);
                        }
                        else if (result.SendingException != null)
                        {
                            _queue.Logger.LogError($"An error has occurred adding job {this} into the queue{System.Environment.NewLine}{result.SendingException}");
                            RaiseException(result.SendingException);
                        }
                    }
                    catch (Exception ex)
                    {
                        _queue.Logger.LogError($"A fatal error has occurred trying to add job {this} into the queue{System.Environment.NewLine}{ex}");
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
        private void RaiseNonFatalFailureEnQueue(IJobQueueOutputMessage message)
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
