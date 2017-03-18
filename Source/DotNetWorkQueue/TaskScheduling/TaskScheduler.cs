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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Amib.Threading;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.TaskScheduling
{
    /// <summary>
    /// A task scheduler for <see cref="IConsumerQueueScheduler"/>
    /// <remarks>This uses <see cref="SmartThreadPool"/> https://github.com/amibar/SmartThreadPool to handle the threads internally</remarks>
    /// </summary>
    public class SmartThreadPoolTaskScheduler : ATaskScheduler
    {
        private readonly ITaskSchedulerConfiguration _configuration;
        private SmartThreadPool _smartThreadPool;
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
        public SmartThreadPoolTaskScheduler(ITaskSchedulerConfiguration configuration, 
            IWaitForEventOrCancelThreadPool waitForFreeThread,
            IMetrics metrics)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => waitForFreeThread, waitForFreeThread);
            Guard.NotNull(() => metrics, metrics);

            _configuration = configuration;
            _waitForFreeThread = waitForFreeThread;
            _metrics = metrics;
            _groups = new ConcurrentDictionary<IWorkGroup, WorkGroupWithItem>();
            _clients = new ConcurrentDictionary<int, int>();

            var name = GetType().Name;
            _taskCounter = metrics.Counter($"{name}.TaskCounter", Units.Items);
            _clientCounter = metrics.Counter($"{name}.ClientCounter", Units.Items);
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public override ITaskSchedulerConfiguration Configuration { get { ThrowIfDisposed(); return _configuration; } }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        /// <exception cref="DotNetWorkQueueException">Start must only be called 1 time</exception>
        public override void Start()
        {
            ThrowIfDisposed();
            Guard.IsValid(() => Configuration.MaximumThreads, Configuration.MaximumThreads, i => i > 0,
              "The Configuration.MaximumThreads must be greater than 0");

            if (_smartThreadPool != null)
            {
                throw new DotNetWorkQueueException("Start must only be called 1 time");    
            }
          
            var stpStartInfo = new STPStartInfo
            {
                IdleTimeout = Convert.ToInt32(_configuration.ThreadIdleTimeout.TotalMilliseconds),
                MaxWorkerThreads = _configuration.MaximumThreads,
                MinWorkerThreads = _configuration.MinimumThreads,
                PostExecuteWorkItemCallback = PostExecuteWorkItemCallback
            };
            _smartThreadPool = new SmartThreadPool(stpStartInfo);
            Configuration.SetReadOnly();
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="ATaskScheduler"/> is started.
        /// </summary>
        /// <value>
        ///   <c>true</c> if started; otherwise, <c>false</c>.
        /// </value>
        public override bool Started => _smartThreadPool != null;

        /// <summary>
        /// If true, the task scheduler has room for another task.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [room for new task]; otherwise, <c>false</c>.
        /// </value>
        /// <exception cref="DotNetWorkQueueException">Start must be called on the scheduler before adding tasks</exception>
        /// <remarks>
        /// This could mean that a thread is free, or that an in memory queue has room.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "need to throw exception if scheduler is not running")]
        public override RoomForNewTaskResult RoomForNewTask
        {
            get
            {
                if (IsDisposed)
                    return RoomForNewTaskResult.No;

                if (_smartThreadPool == null)
                    throw new DotNetWorkQueueException("Start must be called on the scheduler before adding tasks");

                if (HaveRoomForTask)
                {
                    return CurrentTaskCount > _smartThreadPool.MaxThreads ? RoomForNewTaskResult.RoomInQueue : RoomForNewTaskResult.RoomForTask;
                }
                return RoomForNewTaskResult.No;
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
            Interlocked.Increment(ref _groups[group].CurrentWorkItems);
        }

        /// <summary>
        /// Increments the counter for the running tasks
        /// </summary>
        protected virtual void IncrementCounter()
        {
            Interlocked.Increment(ref _currentTaskCount);
        }

        /// <summary>
        /// De-increments the counter for the running tasks
        /// </summary>
        protected virtual void DeincrementCounter()
        {
            Interlocked.Decrement(ref _currentTaskCount);
        }

        /// <summary>
        /// De-increments the task counter for a specific group.
        /// </summary>
        /// <param name="group">The group.</param>
        protected virtual void DeincrementGroup(IWorkGroup group)
        {
            Interlocked.Decrement(ref _groups[group].CurrentWorkItems);
        }

        /// <summary>
        /// If true, the task scheduler has room for the specified work group task
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns></returns>
        public override RoomForNewTaskResult RoomForNewWorkGroupTask(IWorkGroup group)
        {
            if (IsDisposed)
                return RoomForNewTaskResult.No;

            if (HaveRoomForWorkGroupTask(group))
            {
                return CurrentTaskCount > _groups[group].GroupInfo.ConcurrencyLevel ? RoomForNewTaskResult.RoomInQueue : RoomForNewTaskResult.RoomForTask;
            }
            return RoomForNewTaskResult.No;
        }

        /// <summary>
        /// Adds a new task to the scheduler.
        /// </summary>
        /// <param name="task">The task.</param>
        public override void AddTask(Task task)
        {
            QueueTask(task);
        }

        /// <summary>
        /// Informs the scheduler that it has another client connected
        /// </summary>
        public override int Subscribe()
        {
            var id = Interlocked.Increment(ref _nextClientId);
            if (_clients.TryAdd(id, id))
            {
                _clientCounter.Increment(1);
            }
            return id;
        }

        /// <summary>
        /// Informs the scheduler that a client has disconnected
        /// </summary>
        /// <param name="id">The client identifier.</param>
        public override void UnSubscribe(int id)
        {
            int temp;
            if (_clients.TryRemove(id, out temp))
            {
                _clientCounter.Decrement(1);
            }
        }

        /// <summary>
        /// Queues a <see cref="T:System.Threading.Tasks.Task" /> to the scheduler.
        /// </summary>
        /// <param name="task">The <see cref="T:System.Threading.Tasks.Task" /> to be queued.</param>
        protected sealed override void QueueTask(Task task)
        {
            ThrowIfDisposed();

            var information = task.AsyncState as StateInformation;
            if (information != null)
            {
                var state = information;
                if (state.Group != null)
                {
                    IncrementCounter();
                    IncrementGroup(state.Group);
                    _groups[state.Group].MetricCounter.Increment(1);
                    _taskCounter.Increment(1);
                    SetWaitHandle(state.Group);
                    _groups[state.Group].Group.QueueWorkItem(() => TryExecuteTaskWrapped(task, state));
                }
                else
                {
                    IncrementCounter();
                    _taskCounter.Increment(1);
                    SetWaitHandle(null);
                    _smartThreadPool.QueueWorkItem(() => TryExecuteTask(task));
                }
            }
            else
            {
                IncrementCounter();
                _taskCounter.Increment(1);
                SetWaitHandle(null);
                _smartThreadPool.QueueWorkItem(() => TryExecuteTask(task));
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
        public sealed override int MaximumConcurrencyLevel => _smartThreadPool.MaxThreads + _configuration.MaxQueueSize;

        /// <summary>
        /// Adds a new work group.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="concurrencyLevel">The concurrency level.</param>
        /// <returns></returns>
        public override IWorkGroup AddWorkGroup(string name, int concurrencyLevel)
        {
            ThrowIfDisposed();
            return AddWorkGroup(name, concurrencyLevel, 0);
        }

        /// <summary>
        /// Adds a new work group.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="concurrencyLevel">The concurrency level.</param>
        /// <param name="maxQueueSize">Maximum size of the queue. Work groups have a queue that is separate per queue, and is not shared with non work group items</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">Start must be called on the scheduler before adding work groups</exception>
        public override IWorkGroup AddWorkGroup(string name, int concurrencyLevel, int maxQueueSize)
        {
            ThrowIfDisposed();

            if (_smartThreadPool == null)
                throw new DotNetWorkQueueException("Start must be called on the scheduler before adding work groups");

            var group = new WorkGroup(name, concurrencyLevel, maxQueueSize);
            if (_groups.ContainsKey(group)) return _groups[group].GroupInfo;

            var startInfo = new WIGStartInfo
            {
                PostExecuteWorkItemCallback = PostExecuteWorkItemCallback
            };
            var groupWithItem = new WorkGroupWithItem(group, _smartThreadPool.CreateWorkItemsGroup(concurrencyLevel, startInfo), _metrics.Counter(
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
        /// <param name="wir">The work item results</param>
        private void PostExecuteWorkItemCallback(IWorkItemResult wir)
        {
            var possibleState = wir.GetResult();
            var information = possibleState as StateInformation;
            if (information != null) //if not null, this is a work group
            {
                var state = information;
                DeincrementCounter();
                DeincrementGroup(state.Group);
                _groups[state.Group].MetricCounter.Decrement(1);
                _taskCounter.Decrement(_groups[state.Group].Group.Name, 1);
                SetWaitHandle(state.Group);   
            }
            else //is null, so this is not a work group item
            {
                DeincrementCounter();
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

            if (_smartThreadPool != null)
            {
                _smartThreadPool.WaitForIdle(_configuration.WaitForTheadPoolToFinish);
                _smartThreadPool.Shutdown();
                _smartThreadPool.Dispose();
                _smartThreadPool = null;
            }
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
