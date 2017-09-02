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
using System.Runtime.CompilerServices;
using System.Threading;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Allows workers to be paused, and suspend looking for work until awoken
    /// </summary>
    internal class WorkerWaitForEventOrCancel : IWorkerWaitForEventOrCancel
    {
        private readonly IWaitForEventOrCancelWorker _waitForEventOrCancel;
        private readonly IWorkerConfiguration _workerConfiguration;

        private int _disposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerWaitForEventOrCancel"/> class.
        /// </summary>
        /// <param name="waitForEventOrCancel">The wait for event or cancel.</param>
        /// <param name="workerConfiguration">The worker configuration.</param>
        public WorkerWaitForEventOrCancel(IWaitForEventOrCancelWorker waitForEventOrCancel,
            IWorkerConfiguration workerConfiguration)
        {
            _waitForEventOrCancel = waitForEventOrCancel;
            _workerConfiguration = workerConfiguration;
        }

        /// <summary>
        /// Waits until notified to stop waiting.
        /// </summary>
        /// <returns></returns>
        public bool Wait()
        {
            ThrowIfDisposed();
            if (_workerConfiguration.WorkerCount == 1 || !_workerConfiguration.SingleWorkerWhenNoWorkFound)
            {
                //don't block if there is only 1 worker, or if single worker queue discovery is disabled
                return false;
            }

            //if we are not paused, this will just return
            return _waitForEventOrCancel.Wait();
        }

        /// <summary>
        /// Resets the wait status, causing <see cref="Wait" /> calls to wait.
        /// </summary>
        public void Reset()
        {
            ThrowIfDisposed();
            _waitForEventOrCancel.Reset();
        }

        /// <summary>
        /// Sets the state to signaled; any <see cref="Wait" /> calls will return
        /// </summary>
        public void Set()
        {
            ThrowIfDisposed();
            _waitForEventOrCancel.Set();
        }

        /// <summary>
        /// Cancels any current <see cref="Wait" /> calls
        /// </summary>
        public void Cancel()
        {
            ThrowIfDisposed();
            _waitForEventOrCancel.Cancel();
        }

        #region IDisposable, IsDisposed
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
            Interlocked.Increment(ref _disposeCount);
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
