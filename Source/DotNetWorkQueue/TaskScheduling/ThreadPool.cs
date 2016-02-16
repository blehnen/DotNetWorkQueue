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
using Amib.Threading;
using DotNetWorkQueue.Exceptions;
using Action = System.Action;
namespace DotNetWorkQueue.TaskScheduling
{
    /// <summary>
    /// A thread pool for the queue. <see cref="SmartThreadPool"/> is used internally.
    /// </summary>
    public class ThreadPool : IThreadPool
    {
        private SmartThreadPool _threadPool;
        private readonly ThreadPoolConfiguration _configuration;
        private int _disposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadPool"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public ThreadPool(ThreadPoolConfiguration configuration)
        {
            Guard.NotNull(() => configuration, configuration);
            _configuration = configuration;
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            ThrowIfDisposed();
            if (_threadPool != null)
            {
                throw new DotNetWorkQueueException("Start must only be called 1 time");
            }

            if (_configuration.MaxWorkerThreads > 0)
            {
                _threadPool = new SmartThreadPool(Convert.ToInt32(_configuration.IdleTimeout.TotalMilliseconds),
                    _configuration.MaxWorkerThreads, _configuration.MinWorkerThreads);
            }
        }
        /// <summary>
        /// Gets a value indicating whether this instance is shutting down.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is shutting down; otherwise, <c>false</c>.
        /// </value>
        public bool IsShuttingdown
        {
            get
            {
                if (IsDisposed)
                    return true;

                return _threadPool != null && _threadPool.IsShuttingdown;
            }
        }

        /// <summary>
        /// Queues a work item.
        /// </summary>
        /// <param name="action">The work item to queue.</param>
        public void QueueWorkItem(Action action)
        {
            ThrowIfDisposed();
            _threadPool?.QueueWorkItem(new Amib.Threading.Action(action));
        }

        /// <summary>
        /// Gets a value indicating whether this instance is started.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is started; otherwise, <c>false</c>.
        /// </value>
        public bool IsStarted => _threadPool != null;

        /// <summary>
        /// Gets the active threads count.
        /// </summary>
        /// <value>
        /// The active threads.
        /// </value>
        public int ActiveThreads
        {
            get
            {
                if (IsDisposed)
                    return 0;

                return _threadPool?.ActiveThreads ?? 0;
            }
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

            if (Interlocked.Increment(ref _disposeCount) != 1 || _threadPool == null) return;

            _threadPool.Cancel(false);
            _threadPool.WaitForIdle(_configuration.WaitForTheadPoolToFinish);
            _threadPool.Shutdown();
            _threadPool.Dispose();
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
