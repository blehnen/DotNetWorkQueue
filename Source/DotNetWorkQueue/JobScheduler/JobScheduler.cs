// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.JobScheduler
{
    /// <inheritdoc />
    public class JobScheduler: IJobScheduler
    {
        private readonly object _lockTasks = new object();
        private readonly Dictionary<string, ScheduledJob> _tasks = new Dictionary<string, ScheduledJob>();
        private readonly object _lockHeap = new object();
        private readonly PendingEventHeap _eventHeap = new PendingEventHeap();

        /// <inheritdoc />
        public bool IsShuttingDown { get; private set; }

        /// <summary>
        /// Occurs when a job has thrown an exception when being added to the execution queue.
        /// </summary>
        public event Action<IScheduledJob, Exception> OnJobQueueException;
        /// <summary>
        /// Occurs when a job can't be added to the queue; this generally means that a previous job is still running or has already finished for the same scheduled time.
        /// </summary>
        public event Action<IScheduledJob, IJobQueueOutputMessage> OnJobNonFatalFailureQueue;
        /// <summary>
        /// Occurs when a job has been added to the execution queue.
        /// </summary>
        public event Action<IScheduledJob, IJobQueueOutputMessage> OnJobQueue;

        private readonly IJobQueue _jobQueue;
        private readonly IGetTimeFactory _getTime;
        private readonly ILogFactory _logFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobScheduler"/> class.
        /// </summary>
        /// <param name="jobQueue">The job queue.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        /// <param name="logFactory">The log factory.</param>
        public JobScheduler(IJobQueue jobQueue,
            IGetTimeFactory getTimeFactory,
            ILogFactory logFactory)
        {
            _jobQueue = jobQueue;
            _getTime = getTimeFactory;
            _logFactory = logFactory;
        }

        /// <inheritdoc />
        public void Start()
        {
            //log task time (from time factory) and local machine time to show differences..
            var log = _logFactory.Create("TIME");
            log.Log(LogLevel.Info, () => $"Scheduler time is {_getTime.Create().GetCurrentUtcDate()}");
            log.Log(LogLevel.Info, () => $"Local time is {DateTime.UtcNow}");

            Task.Run(PollAsync);
        }
        /// <summary>
        /// Adds a new job or updates an existing job.
        /// </summary>
        /// <typeparam name="TTransportInit">The type of the transport initialize.</typeparam>
        /// <typeparam name="TQueue">The type of the queue.</typeparam>
        /// <param name="jobname">The jobname.</param>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="schedule">The schedule.</param>
        /// <param name="actionToRun">The action to run.</param>
        /// <param name="route">The route.</param>
        /// <param name="producerConfiguration">The producer configuration.</param>
        /// <param name="autoRun">if set to <c>true</c> [automatic run].</param>
        /// <param name="window">The window.</param>
        /// <returns></returns>
        public IScheduledJob AddUpdateJob<TTransportInit, TQueue>(
            string jobname,
            string queue,
            string connection,
            string schedule,
            LinqExpressionToRun actionToRun,
            string route = null,
            Action<QueueProducerConfiguration> producerConfiguration = null,
            bool autoRun = true,
            TimeSpan window = default(TimeSpan))
             where TTransportInit : ITransportInit, new()
             where TQueue : class, IJobQueueCreation
        {
            Guard.NotNullOrEmpty(() => schedule, schedule);
            Guard.IsValid(() => jobname, jobname, i => i.Length < 256,
               "The job name length must be 255 characters or less");

            return AddTaskImpl<TTransportInit, TQueue>(jobname, queue, connection, new JobSchedule(schedule, GetCurrentOffset), autoRun, window, null, actionToRun, route, false, producerConfiguration);
        }

        /// <summary>
        /// Adds a new job or updates an existing job.
        /// </summary>
        /// <typeparam name="TTransportInit">The type of the transport initialize.</typeparam>
        /// <param name="jobQueueCreation">The job queue creation.</param>
        /// <param name="jobname">The jobname.</param>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="schedule">The schedule.</param>
        /// <param name="actionToRun">The action to run.</param>
        /// <param name="route">The route.</param>
        /// <param name="producerConfiguration">The producer configuration.</param>
        /// <param name="autoRun">if set to <c>true</c> [automatic run].</param>
        /// <param name="window">The window.</param>
        /// <returns></returns>
        public IScheduledJob AddUpdateJob<TTransportInit>(
            IJobQueueCreation jobQueueCreation,
            string jobname,
            string queue,
            string connection,
            string schedule,
            LinqExpressionToRun actionToRun,
            string route = null,
            Action<QueueProducerConfiguration> producerConfiguration = null,
            bool autoRun = true,
            TimeSpan window = default(TimeSpan))
             where TTransportInit : ITransportInit, new()
        {
            Guard.NotNullOrEmpty(() => schedule, schedule);
            Guard.IsValid(() => jobname, jobname, i => i.Length < 256,
               "The job name length must be 255 characters or less");

            return AddTaskImpl<TTransportInit>(jobQueueCreation, jobname, queue, connection, new JobSchedule(schedule, GetCurrentOffset), autoRun, window, null, actionToRun, route, false, producerConfiguration);
        }

        /// <inheritdoc />
        public IScheduledJob AddUpdateJob<TTransportInit, TQueue>(
            string jobName,
            string queue,
            string connection,
            string schedule,
            Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> actionToRun,
            string route = null,
            Action<QueueProducerConfiguration> producerConfiguration = null,
            bool autoRun = true,
            TimeSpan window = default(TimeSpan),
            bool rawExpression = false)
             where TTransportInit : ITransportInit, new()
             where TQueue : class, IJobQueueCreation
        {
            Guard.NotNullOrEmpty(() => schedule, schedule);
            Guard.IsValid(() => jobName, jobName, i => i.Length < 256,
              "The job name length must be 255 characters or less");
            return AddTaskImpl<TTransportInit, TQueue>(jobName, queue, connection, new JobSchedule(schedule, GetCurrentOffset), autoRun, window, actionToRun, null, route, rawExpression, producerConfiguration);
        }
        /// <inheritdoc />
        public IScheduledJob AddUpdateJob<TTransportInit>(
            IJobQueueCreation jobQueueCreation,
            string jobname,
            string queue,
            string connection,
            string schedule,
            Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> actionToRun,
            string route = null,
            Action<QueueProducerConfiguration> producerConfiguration = null,
            bool autoRun = true,
            TimeSpan window = default(TimeSpan),
            bool rawExpression = false)
             where TTransportInit : ITransportInit, new()
        {
            Guard.NotNullOrEmpty(() => schedule, schedule);
            Guard.IsValid(() => jobname, jobname, i => i.Length < 256,
              "The job name length must be 255 characters or less");
            return AddTaskImpl<TTransportInit>(jobQueueCreation, jobname, queue, connection, new JobSchedule(schedule, GetCurrentOffset), autoRun, window, actionToRun, null, route, rawExpression, producerConfiguration);
        }
        /// <summary>
        /// Adds the task
        /// </summary>
        /// <typeparam name="TTransportInit">The type of the transport initialize.</typeparam>
        /// <typeparam name="TQueue">The type of the queue.</typeparam>
        /// <param name="name">The name.</param>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="schedule">The schedule.</param>
        /// <param name="autoRun">if set to <c>true</c> [automatic run].</param>
        /// <param name="window">The window.</param>
        /// <param name="actionToRun">The action to run.</param>
        /// <param name="expressionToRun">The expression to run.</param>
        /// <param name="route">The route.</param>
        /// <param name="rawExpression">if set to <c>true</c> this expression will not be serialized. This will fail unless an in-process queue is being used.</param>
        /// <param name="producerConfiguration">The producer configuration.</param>
        /// <returns></returns>
        /// <exception cref="JobSchedulerException">Cannot add a task after Shutdown has been called.</exception>
        private ScheduledJob AddTaskImpl<TTransportInit,TQueue>(
            string name,
            string queue,
            string connection,
            IJobSchedule schedule,
            bool autoRun,
            TimeSpan window,
            Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> actionToRun,
            LinqExpressionToRun expressionToRun,
            string route,
            bool rawExpression,
            Action<QueueProducerConfiguration> producerConfiguration = null)
                where TTransportInit : ITransportInit, new()
                where TQueue : class, IJobQueueCreation
        {
            Guard.NotNull(() => schedule, schedule);
            Guard.NotNullOrEmpty(() => name, name);


            ScheduledJob job;
            lock (_lockTasks)
            {
                if (IsShuttingDown)
                    throw new JobSchedulerException("Cannot add a task after Shutdown has been called.");

                if (_tasks.ContainsKey(name))
                {
                    RemoveJob(name);
                }
                if (expressionToRun != null)
                {
                    job = new ScheduledJob(this, name, schedule, _jobQueue.Get<TTransportInit, TQueue>(queue, connection, producerConfiguration), expressionToRun, _getTime.Create(), route)
                    {
                        Window = window,
                        IsAttached = true
                    };
                    _tasks.Add(name, job);
                }
                else
                {
                    job = new ScheduledJob(this, name, schedule, _jobQueue.Get<TTransportInit, TQueue>(queue, connection, producerConfiguration), actionToRun, _getTime.Create(), route, rawExpression)
                    {
                        Window = window,
                        IsAttached = true
                    };
                    _tasks.Add(name, job);
                }
            }

            job.OnException += TaskOnOnException;
            job.OnEnQueue += JobOnOnEnQueue;
            job.OnNonFatalFailureEnQueue += JobOnOnNonFatalFailureEnQueue;
            if (autoRun)
                job.StartSchedule();

            return job;
        }

        /// <summary>
        /// Adds the task.
        /// </summary>
        /// <typeparam name="TTransportInit">The type of the transport initialize.</typeparam>
        /// <param name="jobQueueCreation">The job queue creation.</param>
        /// <param name="name">The name.</param>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="schedule">The schedule.</param>
        /// <param name="autoRun">if set to <c>true</c> [automatic run].</param>
        /// <param name="window">The window.</param>
        /// <param name="actionToRun">The action to run.</param>
        /// <param name="expressionToRun">The expression to run.</param>
        /// <param name="route">The route.</param>
        /// <param name="rawExpression">if set to <c>true</c> this expression will not be serialized. This will fail unless an in-process queue is being used.</param>
        /// <param name="producerConfiguration">The producer configuration.</param>
        /// <returns></returns>
        /// <exception cref="JobSchedulerException">Cannot add a task after Shutdown has been called.</exception>
        private ScheduledJob AddTaskImpl<TTransportInit>(
            IJobQueueCreation jobQueueCreation,
            string name,
            string queue,
            string connection,
            IJobSchedule schedule,
            bool autoRun,
            TimeSpan window,
            Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> actionToRun,
            LinqExpressionToRun expressionToRun,
            string route,
            bool rawExpression,
            Action<QueueProducerConfiguration> producerConfiguration = null)
                where TTransportInit : ITransportInit, new()
        {
            Guard.NotNull(() => schedule, schedule);
            Guard.NotNullOrEmpty(() => name, name);

            ScheduledJob job;
            lock (_lockTasks)
            {
                if (IsShuttingDown)
                    throw new JobSchedulerException("Cannot add a task after Shutdown has been called.");

                if (_tasks.ContainsKey(name))
                {
                    RemoveJob(name);
                }
                if (expressionToRun != null)
                {
                    job = new ScheduledJob(this, name, schedule, _jobQueue.Get<TTransportInit>(jobQueueCreation, queue, connection, producerConfiguration), expressionToRun, _getTime.Create(), route)
                    {
                        Window = window,
                        IsAttached = true
                    };
                    _tasks.Add(name, job);
                }
                else
                {
                    job = new ScheduledJob(this, name, schedule, _jobQueue.Get<TTransportInit>(jobQueueCreation, queue, connection, producerConfiguration), actionToRun, _getTime.Create(), route, rawExpression)
                    {
                        Window = window,
                        IsAttached = true
                    };
                    _tasks.Add(name, job);
                }
            }

            job.OnException += TaskOnOnException;
            job.OnEnQueue += JobOnOnEnQueue;
            job.OnNonFatalFailureEnQueue += JobOnOnNonFatalFailureEnQueue;
            if (autoRun)
                job.StartSchedule();

            return job;
        }

        /// <summary>
        /// Gets the current offset.
        /// </summary>
        /// <returns></returns>
        private DateTimeOffset GetCurrentOffset()
        {
            return new DateTimeOffset(_getTime.Create().GetCurrentUtcDate());
        }

        private void JobOnOnNonFatalFailureEnQueue(IScheduledJob scheduledJob, IJobQueueOutputMessage jobQueueOutputMessage)
        {
            var ev = OnJobNonFatalFailureQueue;
            ev?.Invoke(scheduledJob, jobQueueOutputMessage);
        }

        private void JobOnOnEnQueue(IScheduledJob scheduledJob, IJobQueueOutputMessage jobQueueOutputMessage)
        {
            var ev = OnJobQueue;
            ev?.Invoke(scheduledJob, jobQueueOutputMessage);
        }

        private void TaskOnOnException(IScheduledJob task, Exception ex)
        {
            var ev = OnJobQueueException;
            ev?.Invoke(task, ex);
        }

        /// <summary>
        /// Gets all jobs.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IScheduledJob> GetAllJobs()
        {
            lock (_lockTasks)
            {
                // could be one line of linq, but eh, this is cheaper
                var tasks = new ScheduledJob[_tasks.Count];
                var i = 0;
                foreach (var t in _tasks)
                {
                    tasks[i] = t.Value;
                    i++;
                }

                return tasks;
            }
        }

        /// <summary>
        /// Removes the job from the scheduler
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        /// <exception cref="JobSchedulerException">
        /// Cannot remove a task from after Shutdown has been called.
        /// or
        /// Cannot remove task \"{name}\
        /// </exception>
        public bool RemoveJob(string name)
        {
            lock (_lockTasks)
            {
                if (IsShuttingDown)
                    throw new JobSchedulerException("Cannot remove a task from after Shutdown has been called.");

                if (!_tasks.TryGetValue(name, out var task))
                    return false;

                if (task.IsScheduleRunning)
                    throw new JobSchedulerException($"Cannot remove task \"{name}\". It is still running.");

                task.IsAttached = false;
                _tasks.Remove(name);
                return true;
            }
        }


        internal void AddPendingEvent(PendingEvent ev)
        {
            if (IsShuttingDown) // don't care about adding anything if we're shutting down
                return;

            lock (_lockHeap)
            {
                _eventHeap.Push(ev);
            }
        }

        private async Task PollAsync()
        {
            // figure out the initial delay
            var now = new DateTimeOffset(_getTime.Create().GetCurrentUtcDate());
            var intendedTime = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, now.Offset);
            if (now.Millisecond > 0)
            {
                await Task.Delay(1000 - now.Millisecond).ConfigureAwait(false);
                intendedTime = intendedTime.AddSeconds(1);
            }

            while (true)
            {
                if (IsShuttingDown)
                    return;

                PopAndRunEvents(intendedTime);

                // figure out the next second to poll on
                now = new DateTimeOffset(_getTime.Create().GetCurrentUtcDate());
                do
                {
                    intendedTime = intendedTime.AddSeconds(1);
                }
                while (intendedTime < now);

                await Task.Delay(intendedTime - now).ConfigureAwait(false);
            }
        }

        private void PopAndRunEvents(DateTimeOffset intendedTime)
        {
            lock (_lockHeap)
            {
                while (_eventHeap.Count > 0 && _eventHeap.Peek().ScheduledTime <= intendedTime)
                {
                    _eventHeap.Pop().Run(); // queues for running on the thread pool
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => _disposedValue;

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls
        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    IScheduledJob[] tasks;
                    lock (_lockTasks)
                    {
                        IsShuttingDown = true;
                        tasks = GetAllJobs().ToArray();
                    }

                    foreach (var t in tasks)
                    {
                        t.IsAttached = false; // prevent anyone from calling start on the task again
                        t.StopSchedule();
                    }

                    while (true)
                    {
                        var allStopped = true;
                        foreach (var t in tasks)
                        {
                            if (t.IsCallbackExecuting)
                            {
                                allStopped = false;
                                break;
                            }
                        }

                        if (allStopped)
                            return;

                        Thread.Sleep(10); // wait 10 milliseconds, then check again
                    }
                }
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
