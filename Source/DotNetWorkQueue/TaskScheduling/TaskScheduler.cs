// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetWorkQueue.TaskScheduling
{
    /// <inheritdoc />
    public class SmartThreadPoolTaskScheduler : ATaskScheduler
    {
        private readonly ITaskSchedulerConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<IWorkGroup, WorkGroupWithItem> _groups;
        private readonly ConcurrentDictionary<int, int> _clients;
        private readonly IWaitForEventOrCancelThreadPool _waitForFreeThread;
        private readonly IMetrics _metrics;
        private readonly ICounter _taskCounter;
        private readonly ICounter _clientCounter;

        private int _disposeCount;
        private long _currentTaskCount;
        private int _nextClientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartThreadPoolTaskScheduler"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="waitForFreeThread">The wait for free thread.</param>
        /// <param name="metrics">the metrics factory</param>
        /// <param name="log">the logger</param>
        public SmartThreadPoolTaskScheduler(ITaskSchedulerConfiguration configuration,
            IWaitForEventOrCancelThreadPool waitForFreeThread,
            IMetrics metrics,
            ILogger log)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => waitForFreeThread, waitForFreeThread);
            Guard.NotNull(() => metrics, metrics);
            Guard.NotNull(() => log, log);

            _logger = log;
            _configuration = configuration;
            _waitForFreeThread = waitForFreeThread;
            _metrics = metrics;
            _groups = new ConcurrentDictionary<IWorkGroup, WorkGroupWithItem>();
            _clients = new ConcurrentDictionary<int, int>();

            var name = GetType().Name;
            _taskCounter = metrics.Counter($"{name}.TaskCounter", Units.Items);
            _clientCounter = metrics.Counter($"{name}.ClientCounter", Units.Items);
        }

        /// <inheritdoc />
        public override ITaskSchedulerConfiguration Configuration { get { ThrowIfDisposed(); return _configuration; } }

        /// <inheritdoc />
        public override void Start()
        {
            ThrowIfDisposed();
            Guard.IsValid(() => Configuration.MaximumThreads, Configuration.MaximumThreads, i => i > 0,
              "The Configuration.MaximumThreads must be greater than 0");

            Configuration.SetReadOnly();
        }

        /// <inheritdoc />
        public override bool Started => _configuration.IsReadOnly;

        /// <inheritdoc />
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "need to throw exception if scheduler is not running")]
        public override RoomForNewTaskResult RoomForNewTask
        {
            get
            {
                if (IsDisposed)
                    return RoomForNewTaskResult.No;

                if (!Configuration.IsReadOnly)
                    throw new DotNetWorkQueueException("The scheduler has not been started; Call Start first");

                return HaveRoomForTask ? RoomForNewTaskResult.RoomForTask : RoomForNewTaskResult.No;
            }
        }

        /// <summary>
        /// Returns true if there is room to add a task
        /// </summary>
        /// <value>
        ///   <c>true</c> if [have room for task]; otherwise, <c>false</c>.
        /// </value>
        protected virtual bool HaveRoomForTask => Interlocked.CompareExchange(ref _currentTaskCount, 0, 0) < MaximumConcurrencyLevel;

        /// <summary>
        /// Gets the current task count.
        /// </summary>
        /// <value>
        /// The current task count.
        /// </value>
        protected virtual long CurrentTaskCount => Interlocked.Read(ref _currentTaskCount);

        /// <summary>
        /// Returns true if the work group has room for a new task
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns></returns>
        protected virtual bool HaveRoomForWorkGroupTask(IWorkGroup group)
        {
            return Interlocked.CompareExchange(ref _groups[group].CurrentWorkItems, 0, 0) < _groups[group].MaxWorkItems;
        }

        /// <summary>
        /// Increments the task count for a specific group
        /// </summary>
        /// <param name="group">The group.</param>
        protected virtual void IncrementGroup(IWorkGroup group)
        {
            var current = Interlocked.Increment(ref _groups[group].CurrentWorkItems);

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.Log(LogLevel.Trace, $"Task count for group {group.Name} is {current}");

                var queue = Interlocked.Read(ref _currentTaskCount);
                _logger.Log(LogLevel.Trace, $"Task count is {queue} with the max being {_configuration.MaximumThreads}");
            }
        }

        /// <summary>
        /// Increments the counter for the running tasks
        /// </summary>
        protected virtual void IncrementCounter()
        {
            var current = Interlocked.Increment(ref _currentTaskCount);

            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.Log(LogLevel.Trace, $"Task count is {current} with the max being {_configuration.MaximumThreads}");
        }

        /// <summary>
        /// De-increments the counter for the running tasks
        /// </summary>
        protected virtual void DecrementCounter()
        {
            var current = Interlocked.Decrement(ref _currentTaskCount);

            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.Log(LogLevel.Trace, $"Task count is {current} with the max being {_configuration.MaximumThreads}");
        }

        /// <summary>
        /// De-increments the task counter for a specific group.
        /// </summary>
        /// <param name="group">The group.</param>
        protected virtual void DecrementGroup(IWorkGroup group)
        {
            var current = Interlocked.Decrement(ref _groups[group].CurrentWorkItems);
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.Log(LogLevel.Trace, $"Task count for group {group.Name} is {current}");

                var queue = Interlocked.Read(ref _currentTaskCount);
                _logger.Log(LogLevel.Trace, $"Task count is {queue} with the max being {_configuration.MaximumThreads}");
            }
        }

        /// <inheritdoc />
        public override RoomForNewTaskResult RoomForNewWorkGroupTask(IWorkGroup group)
        {
            if (IsDisposed)
                return RoomForNewTaskResult.No;

            if (HaveRoomForWorkGroupTask(group))
            {
                return RoomForNewTaskResult.RoomForTask;
            }
            return RoomForNewTaskResult.No;
        }

        /// <inheritdoc />
        /// <param name="task">The task.</param>
        public override void AddTask(Task task)
        {
            QueueTask(task);
        }

        /// <inheritdoc />
        public override int Subscribe()
        {
            var id = Interlocked.Increment(ref _nextClientId);
            if (_clients.TryAdd(id, id))
            {
                _clientCounter.Increment(1);
            }
            return id;
        }

        /// <inheritdoc />
        public override void UnSubscribe(int id)
        {
            if (_clients.TryRemove(id, out _))
            {
                _clientCounter.Decrement(1);
            }
        }

        /// <inheritdoc />
        protected sealed override void QueueTask(Task task)
        {
            ThrowIfDisposed();

            if (task.AsyncState is StateInformation information)
            {
                var state = information;
                if (state.Group != null)
                {
                    IncrementCounter();
                    IncrementGroup(state.Group);
                    _groups[state.Group].MetricCounter.Increment(1);
                    _taskCounter.Increment(1);
                    SetWaitHandle(state.Group);
                    Task.Factory.StartNew(() => TryExecuteTaskWrapped(task, state))
                        .ContinueWith(PostExecuteWorkItemCallback, state);
                }
                else
                {
                    IncrementCounter();
                    _taskCounter.Increment(1);
                    SetWaitHandle(null);
                    Task.Factory.StartNew(() => TryExecuteTask(task))
                        .ContinueWith(PostExecuteWorkItemCallback, state);
                }
            }
            else
            {
                IncrementCounter();
                _taskCounter.Increment(1);
                SetWaitHandle(null);
                Task.Factory.StartNew(() => TryExecuteTask(task)).ContinueWith(PostExecuteWorkItemCallback, null);
            }
        }

        /// <summary>
        /// A wrapper for executing the task, so that we can return the state information back to the caller
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="state">The state.</param>
        /// <returns></returns>
        protected StateInformation TryExecuteTaskWrapped(Task task, StateInformation state)
        {
            TryExecuteTask(task);
            return state;
        }
        /// <summary>
        /// Runs the provided task on the current thread.
        /// </summary>
        /// <param name="task">The task to be executed.</param>
        /// <param name="taskWasPreviouslyQueued">Ignored.</param>
        /// <returns>
        /// Whether the task could be executed on the current thread.
        /// </returns>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            //work group tasks cannot be executed on the current thread
            if (task.AsyncState is StateInformation)
            {
                return false;
            }
            return TryExecuteTask(task);
        }

        /// <summary>
        /// Indicates the maximum concurrency level this <see cref="T:System.Threading.Tasks.TaskScheduler" /> is able to support.
        /// </summary>
        public sealed override int MaximumConcurrencyLevel => _configuration.MaximumThreads;

        /// <summary>
        /// Adds a new work group.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="concurrencyLevel">The concurrency level.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">Start must be called on the scheduler before adding work groups</exception>
        public override IWorkGroup AddWorkGroup(string name, int concurrencyLevel)
        {
            ThrowIfDisposed();

            var group = new WorkGroup(name, concurrencyLevel);
            if (_groups.ContainsKey(group)) return _groups[group].GroupInfo;

            var groupWithItem = new WorkGroupWithItem(group, _metrics.Counter(
                $"work group {name}", Units.Items));
            _groups.TryAdd(group, groupWithItem);
            return groupWithItem.GroupInfo;
        }

        /// <summary>
        /// Gets the tasks currently scheduled to this scheduler.
        /// </summary>
        /// <returns>
        /// An enumerable that allows a debugger to traverse the tasks currently queued to this scheduler.
        /// </returns>
        /// <remarks>
        /// This will always return an empty enumerable, as tasks are launched as soon as they're queued; we also don't want the .net scheduler to mess with our internal queue.
        /// </remarks>
        protected override IEnumerable<Task> GetScheduledTasks() { return Enumerable.Empty<Task>(); }

        /// <summary>
        /// Allows caller to block until a thread is free
        /// </summary>
        /// <remarks>
        /// If there are multiple callers, the wait handle may be freed, but not all pending requests will get into the queue.
        /// </remarks>
        public override IWaitForEventOrCancelThreadPool WaitForFreeThread => _waitForFreeThread;

        /// <summary>
        /// Fires after each task is complete.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <param name="wir">The work item results</param>
        private void PostExecuteWorkItemCallback(Task t, object wir)
        {
            var possibleState = wir;
            if (possibleState is StateInformation information && information.Group != null) //if not null, this is a work group
            {
                var state = information;
                DecrementCounter();
                DecrementGroup(state.Group);
                _groups[state.Group].MetricCounter.Decrement(1);
                _taskCounter.Decrement(_groups[state.Group].GroupInfo.Name, 1);
                SetWaitHandle(state.Group);
            }
            else //is null, so this is not a work group item
            {
                DecrementCounter();
                _taskCounter.Decrement(1);
                SetWaitHandle(null);
            }
        }

        /// <summary>
        /// Sets the wait handle.
        /// </summary>
        /// <param name="group">The group.</param>
        protected void SetWaitHandle(IWorkGroup group)
        {
            if (group == null) //not a work group
            {
                if (HaveRoomForTask)
                {
                    _waitForFreeThread.Set(null);
                }
                else
                {
                    _waitForFreeThread.Reset(null);
                }
            }
            else //work group
            {
                if (HaveRoomForTask && HaveRoomForWorkGroupTask(group))
                {
                    _waitForFreeThread.Set(group);
                }
                else
                {
                    _waitForFreeThread.Reset(group);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public override bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            WaitForFreeThread.Cancel();

            WaitForDelegate.Wait(() => CurrentTaskCount > 0,
                _configuration.WaitForThreadPoolToFinish);
        }
        /// <summary>
        /// Throws an exception if this instance has been disposed.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ObjectDisposedException"></exception>
        protected void ThrowIfDisposed([CallerMemberName] string name = "")
        {
            if (Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0)
            {
                throw new ObjectDisposedException(name);
            }
        }
    }
}
