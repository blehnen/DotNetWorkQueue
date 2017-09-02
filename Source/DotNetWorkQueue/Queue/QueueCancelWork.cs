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
using System.Runtime.CompilerServices;
using System.Threading;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Informs listeners that they should stop looking for new work <see cref="StopWorkToken"/> or cancel existing working <see cref="CancelWorkToken"/>
    /// </summary>
    public class QueueCancelWork : IQueueCancelWork
    {
        private readonly CancellationTokenSource _cancelToken;
        private readonly CancellationTokenSource _stopToken;
        private int _disposeCount;
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueCancelWork"/> class.
        /// </summary>
        public QueueCancelWork()
        {
            _cancelToken = new CancellationTokenSource();
            _stopToken = new CancellationTokenSource();
        }
        /// <summary>
        /// The system is preparing to stop. Don't take on additional work.
        /// </summary>
        /// <value>
        /// The stop work token.
        /// </value>
        public CancellationToken StopWorkToken
        {
            get {ThrowIfDisposed(); return _stopToken.Token; }
        }
        /// <summary>
        /// The system wants to stop. Cancel any work in progress.
        /// </summary>
        /// <value>
        /// The cancel work token.
        /// </value>
        /// <remarks>
        /// Canceling of messages in the middle of processing is up to user code. See <see cref="IWorkerNotification" />
        /// For async processing, messages contained in the in memory queue will be gracefully canceled.
        /// </remarks>
        public CancellationToken CancelWorkToken
        {
            get { ThrowIfDisposed(); return _cancelToken.Token; }
        }
        /// <summary>
        /// Gets the cancellation token source.
        /// </summary>
        /// <value>
        /// The cancellation token source.
        /// </value>
        /// <remarks>
        /// This is used to tell both the queue/workers and user code to cancel the current operation
        /// </remarks>
        public CancellationTokenSource CancellationTokenSource
        {
            get { ThrowIfDisposed(); return _cancelToken; }
        }
        /// <summary>
        /// Gets the stop token source.
        /// </summary>
        /// <value>
        /// The stop token source.
        /// </value>
        /// <remarks>
        /// This is used to tell the queue/workers that they should no longer look for new messages to process
        /// </remarks>
        public CancellationTokenSource StopTokenSource
        {
            get { ThrowIfDisposed(); return _stopToken; }
        }
        /// <summary>
        /// All possible cancel tokens.
        /// </summary>
        /// <value>
        /// The tokens.
        /// </value>
        /// <code>
        /// You can turn the collection of tokens into a unified cancel token.
        /// using (var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(ICancelWork.Tokens.ToArray()))
        /// {
        /// //combinedCts.Token wraps all of the tokens
        /// ...
        /// }
        /// </code>
        public List<CancellationToken> Tokens
        {
            get
            {
                ThrowIfDisposed(); return new List<CancellationToken>(2) { StopWorkToken, CancelWorkToken };
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
            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            _cancelToken.Dispose();
            _stopToken.Dispose();
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
