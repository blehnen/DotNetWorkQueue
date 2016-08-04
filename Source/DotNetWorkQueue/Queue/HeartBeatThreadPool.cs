// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Runtime.CompilerServices;
using System.Threading;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.TaskScheduling;
using DotNetWorkQueue.Validation;
using ThreadPool = DotNetWorkQueue.TaskScheduling.ThreadPool;
namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// A thread pool for the heart beat update.
    /// <remarks>
    /// A dedicated thread pool is used since we don't know what the built in one is being used for.
    /// In addition, if a large amount of workers are being used, we could starve the host process of threads if 
    /// we used the built in pool.
    /// </remarks>
    /// </summary>
    internal class HeartBeatThreadPool : IHeartBeatThreadPool
    {
        private readonly IHeartBeatThreadPoolConfiguration _configuration;
        private ThreadPool _threadPool;
        private int _disposeCount;
        /// <summary>
        /// Initializes a new instance of the <see cref="HeartBeatThreadPool"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public HeartBeatThreadPool(IHeartBeatThreadPoolConfiguration configuration)
        {
            Guard.NotNull(() => configuration, configuration);
            _configuration = configuration;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is shutting down.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is shutting down; otherwise, <c>false</c>.
        /// </value>
        public bool IsShuttingdown => _threadPool != null && _threadPool.IsShuttingdown;

        /// <summary>
        /// Queues a work item.
        /// </summary>
        /// <param name="action">The work item to queue.</param>
        public void QueueWorkItem(Action action)
        {
            Guard.NotNull(() => action, action);

            ThrowIfDisposed();

            if (!IsStarted)
            {
                throw new DotNetWorkQueueException("Start must be called before queuing work items");
            }

            if (_configuration.ThreadsMax == 0)
            {
                throw new DotNetWorkQueueException("The maximum threads are set to 0. No work items can be queued");
            }

            _threadPool.QueueWorkItem(action);
        }

        /// <summary>
        /// Gets the active threads count.
        /// </summary>
        /// <value>
        /// The active threads.
        /// </value>
        public int ActiveThreads => _threadPool?.ActiveThreads ?? 0;

        /// <summary>
        /// Gets a value indicating whether this instance is started.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is started; otherwise, <c>false</c>.
        /// </value>
        public bool IsStarted => _threadPool != null;

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            if (IsStarted)
            {
                throw new DotNetWorkQueueException("Start must only be called 1 time");
            }

            var poolConfiguration = new ThreadPoolConfiguration
            {
                MinWorkerThreads = _configuration.ThreadsMin,
                MaxWorkerThreads = _configuration.ThreadsMax,
                IdleTimeout = _configuration.ThreadIdleTimeout
            };
            _threadPool = new ThreadPool(poolConfiguration);
            _threadPool.Start();
        }

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
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_threadPool", Justification = "not needed")]
        public void Dispose()
        {
            if (Interlocked.Increment(ref _disposeCount) == 1)
            {
                _threadPool?.Dispose();
            }
        }
        #endregion
    }
}
