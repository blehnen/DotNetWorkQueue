// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Threading;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Queue
{
    internal class WaitForEventOrCancel : IWaitForEventOrCancel
    {
        private readonly ManualResetEventSlim _resetEvent;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private int _disposeCount;

        //ManualResetEventSlim has no WaitAsync, so async waiters park on this instead.
        //RunContinuationsAsynchronously matters twice over: a continuation must not run
        //inline on a scheduler thread we need back, and must not run inline while this
        //object holds _asyncSync.
        //Null until the first WaitAsync call: nothing allocates a TaskCompletionSource
        //until an async waiter actually exists, so Set/Reset stay allocation-free on the
        //synchronous-only path (e.g. the saturated TaskScheduler hot path).
        private TaskCompletionSource<bool> _asyncWait;

        //Serialises every transition of the (reset event, completion source, token)
        //triple. Without it the reset event and the completion source can disagree - the
        //event signaled while the source is not - and an async waiter then waits for a
        //Set that has already happened.
        private readonly object _asyncSync = new object();

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
            lock (_asyncSync)
            {
                //false is what the synchronous Wait returns when cancelled; async waiters
                //get the same answer rather than an exception
                _asyncWait?.TrySetResult(false);
            }
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

        /// <inheritdoc />
        public ValueTask<bool> WaitAsync()
        {
            ThrowIfDisposed();

            lock (_asyncSync)
            {
                //Cancellation is terminal: the synchronous Wait returns false for the rest
                //of this object's life once cancelled, because it always consults the
                //token. A Reset after a Cancel would otherwise re-arm the source and leave
                //an async caller waiting on an object that can never be signaled again.
                //Disposal is terminal the same way: Dispose only resolves an _asyncWait
                //that already exists, so a caller racing Dispose before this method has
                //ever created one would otherwise manufacture a fresh, incomplete source
                //that nothing can ever complete - Set/Reset/Cancel all throw once disposed,
                //and Dispose has already made its one pass. Both are checked under the lock
                //so they are atomic with the read/creation of _asyncWait below - otherwise a
                //Cancel or Dispose between the check and the lock can be missed.
                if (_cancellationTokenSource.IsCancellationRequested || IsDisposed)
                    return new ValueTask<bool>(false);

                //Created lazily: seed it from the current event state so a first-ever
                //WaitAsync on an already-signaled instance completes immediately, just
                //like before this was lazy.
                _asyncWait ??= _resetEvent.IsSet ? CreateCompletedWait() : CreateWait();

                return new ValueTask<bool>(_asyncWait.Task);
            }
        }

        /// <summary>
        /// Resets the wait status, causing <see cref="Wait" /> calls to wait.
        /// </summary>
        public void Reset()
        {
            ThrowIfDisposed();
            lock (_asyncSync)
            {
                _resetEvent.Reset();

                //Only re-arm a source that has already completed. Replacing one that still
                //has waiters would orphan them: the next Set would complete the new
                //instance while they went on awaiting the old one. Nothing to do while no
                //async waiter has ever existed.
                if (_asyncWait != null && _asyncWait.Task.IsCompleted)
                    _asyncWait = CreateWait();
            }
        }

        /// <summary>
        /// Sets the state to signaled; any <see cref="Wait" /> calls will return
        /// </summary>
        public void Set()
        {
            ThrowIfDisposed();
            lock (_asyncSync)
            {
                _resetEvent.Set();
                _asyncWait?.TrySetResult(true);
            }
        }

        private static TaskCompletionSource<bool> CreateWait() =>
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private static TaskCompletionSource<bool> CreateCompletedWait()
        {
            var source = CreateWait();
            source.SetResult(true);
            return source;
        }

        #region IDispose, IIsDisposed
        /// <summary>
        /// Throws an exception if this instance has been disposed.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException"></exception>
        protected void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0, this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            GC.SuppressFinalize(this);

            //An async waiter holds only a Task; disposing the underlying primitives does
            //not fault it, so without this it waits forever on an object that is gone.
            lock (_asyncSync)
            {
                _asyncWait?.TrySetResult(false);
            }

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
