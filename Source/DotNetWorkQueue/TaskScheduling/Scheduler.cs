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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Validation;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DotNetWorkQueue.TaskScheduling
{
    /// <summary>
    /// Handles passing messages from an async consumer queue to a task factory for execution
    /// </summary>
    public class Scheduler : IConsumerQueueScheduler
    {
        private readonly IConsumerQueueAsync _queue;
        private readonly ISchedulerMessageHandler _schedulerMessageHandler;
        private readonly Lazy<ITaskFactory> _taskFactory;
        private int _disposeCount;
        private int? _schedulerId;

        /// <summary>
        /// Initializes a new instance of the <see cref="Scheduler" /> class.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="schedulerMessageHandler">The message handler.</param>
        /// <param name="factory">The factory.</param>
        /// <param name="workGroup">The work group.</param>
        public Scheduler(IConsumerQueueAsync queue,
            ISchedulerMessageHandler schedulerMessageHandler,
            ITaskFactoryFactory factory,
            IWorkGroup workGroup)
        {
            Guard.NotNull(() => queue, queue);
            Guard.NotNull(() => schedulerMessageHandler, schedulerMessageHandler);
            Guard.NotNull(() => factory, factory);

            _queue = queue;
            _schedulerMessageHandler = schedulerMessageHandler;
            _taskFactory = new Lazy<ITaskFactory>(factory.Create);

            //if the work group is not set, we are going to just treat it as null
            if (!string.IsNullOrEmpty(workGroup?.Name))
            {
                WorkGroup = workGroup;
            }
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public QueueConsumerConfiguration Configuration
        {
            get
            {
                ThrowIfDisposed();
                return _queue.Configuration;
            }
        }

        /// <summary>
        /// Starts the queue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="functionToRun">The function to run to handle messages.</param>
        /// <param name="notifications">User notifications for the consumer queue processing.</param>
        public void Start<T>(Action<IReceivedMessage<T>, IWorkerNotification> functionToRun, ConsumerQueueNotifications notifications = null)
            where T : class
        {
            ThrowIfDisposed();
            Guard.NotNull(() => functionToRun, functionToRun);

            if (_taskFactory.Value.Scheduler == null)
            {
                throw new DotNetWorkQueueException("A scheduler must be created before starting the queue");
            }

            if (!_taskFactory.Value.Scheduler.Started)
            {
                throw new DotNetWorkQueueException("The scheduler must be started before starting the queue");
            }

            //let the scheduler know it has another client
            if (!_schedulerId.HasValue)
            {
                _schedulerId = _taskFactory.Value.Scheduler.Subscribe();
            }

            _queue.Start<T>(
                (message, notify) =>
                    _schedulerMessageHandler.HandleAsync(WorkGroup, message, notify, functionToRun, _taskFactory.Value), notifications);
        }

        /// <summary>
        /// The task factory that will be used to run message processing threads
        /// </summary>
        /// <value>
        /// The task factory.
        /// </value>
        /// <exception cref="DotNetWorkQueueException">The TaskFactory has already been set once; it cannot be set again</exception>
        public ITaskFactory TaskFactory
        {
            get
            {
                ThrowIfDisposed();
                return _taskFactory.Value;
            }
        }
        /// <summary>
        /// The work group used to limit concurrency.
        /// </summary>
        /// <value>
        /// The work group.
        /// </value>
        /// <remarks>
        /// Not required; may be null. A null instance would mean there are no concurrency requirements
        /// </remarks>
        public IWorkGroup WorkGroup { get; }

        #region IDispose, IIsDisposed
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            //let the scheduler know we are leaving
            if (_taskFactory.IsValueCreated && _taskFactory?.Value.Scheduler != null && _schedulerId.HasValue)
            {
                _taskFactory.Value.Scheduler.UnSubscribe(_schedulerId.Value);
            }

            //explicitly call dispose to force a stop
            _queue.Dispose();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        #endregion
    }
}
