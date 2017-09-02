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
    internal class WaitForEventOrCancel : IWaitForEventOrCancel
    {
        private readonly ManualResetEventSlim _resetEvent;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private int _disposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitForEventOrCancel"/> class.
        /// </summary>
        public WaitForEventOrCancel()
        {
            _resetEvent = new ManualResetEventSlim(true);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Cancels any current <see cref="Wait" /> calls
        /// </summary>
        public void Cancel()
        {
            ThrowIfDisposed();
            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Waits to be notified to stop waiting.
        /// </summary>
        /// <returns></returns>
        public bool Wait()
        {
            ThrowIfDisposed();
            try
            {
                _resetEvent.Wait(_cancellationTokenSource.Token);
                return true;
            }
            catch (OperationCanceledException) //ignore operation canceled exception
            {
                return false;
            }
        }

        /// <summary>
        /// Resets the wait status, causing <see cref="Wait" /> calls to wait.
        /// </summary>
        public void Reset()
        {
            ThrowIfDisposed();
            _resetEvent.Reset();
        }

        /// <summary>
        /// Sets the state to signaled; any <see cref="Wait" /> calls will return
        /// </summary>
        public void Set()
        {
            ThrowIfDisposed();
            _resetEvent.Set();
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
            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            _resetEvent.Dispose();
            _cancellationTokenSource.Dispose();
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
