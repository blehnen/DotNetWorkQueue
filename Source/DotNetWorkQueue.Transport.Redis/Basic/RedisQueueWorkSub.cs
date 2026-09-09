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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Validation;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Allows a caller to wait and block until a pub/sub call has occurred
    /// </summary>
    public class RedisQueueWorkSub : IRedisQueueWorkSub
    {
        #region Member Level Variables
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;
        private ManualResetEventSlim _waitHandle;

        //ManualResetEventSlim has no WaitAsync, so async waiters park on this instead. Every
        //transition of the handle and this source happens together under _asyncSync; splitting them
        //is what produced the lost wake-up and the hang-on-dispose that review caught in the core
        //implementation this mirrors (DotNetWorkQueue.Queue.WaitForEventOrCancel).
        private TaskCompletionSource<bool> _asyncWait;
        private readonly object _asyncSync = new object();
        private readonly ICancelWork _cancelWork;
        private readonly object _setup = new object();
        private bool _ranSetup;

        private int _disposeCount;

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueWorkSub"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        /// <param name="cancelWork">The cancel work.</param>
        public RedisQueueWorkSub(IRedisConnection connection,
            RedisNames redisNames,
            IQueueCancelWork cancelWork)
        {
            Guard.NotNull(connection);
            Guard.NotNull(redisNames);
            Guard.NotNull(cancelWork);

            _connection = connection;
            _redisNames = redisNames;
            _cancelWork = cancelWork;
        }
        #endregion

        /// <summary>
        /// Waits until a notification is received
        /// </summary>
        /// <returns></returns>
        public bool Wait()
        {
            ThrowIfDisposed();

            Setup();
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancelWork.Tokens.ToArray()))
            {
                try
                {
                    _waitHandle.Wait(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        /// <inheritdoc />
        public async ValueTask<bool> WaitAsync(CancellationToken cancellation)
        {
            ThrowIfDisposed();
            Setup();

            Task<bool> wait;
            lock (_asyncSync)
            {
                //Both checks belong inside the lock. Outside it, a cancel or dispose landing between
                //the check and the source being created leaves a waiter that nothing will ever
                //complete.
                if (IsDisposed || _cancelWork.AnyCancellationRequested() || cancellation.IsCancellationRequested)
                    return false;

                _asyncWait ??= _waitHandle.IsSet ? CreateCompletedWait() : CreateWait();
                wait = _asyncWait.Task;
            }

            if (wait.IsCompleted)
                return wait.Result;

            //Mirrors what the synchronous Wait does with its linked token: return false rather than
            //throw, so the receive loop can see the cancellation and return null instead of treating
            //it as a failed de-queue.
            //The caller's token is linked in alongside the queue's own, so a caller that wants to
            //stop waiting can, not just the queue shutting down.
            var tokens = _cancelWork.Tokens.ToList();
            if (cancellation.CanBeCanceled) tokens.Add(cancellation);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(tokens.ToArray());
            await using var registration = cts.Token.Register(CancelAsyncWait).ConfigureAwait(false);
            return await wait.ConfigureAwait(false);
        }

        /// <summary>
        /// Resets the wait handle, so that the next wait blocks until a notification arrives.
        /// </summary>
        public void Reset()
        {
            Setup();
            lock (_asyncSync)
            {
                if (_waitHandle.IsSet)
                    _waitHandle.Reset();

                //Only replace a source that has already completed. Replacing a pending one orphans
                //every waiter holding the old task.
                if (_asyncWait != null && _asyncWait.Task.IsCompleted)
                    _asyncWait = CreateWait();
            }
        }

        private void CancelAsyncWait()
        {
            lock (_asyncSync)
            {
                _asyncWait?.TrySetResult(false);
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

            lock (_setup)
            {
                if (!_ranSetup) return;
            }

            //Release async waiters before the handle goes away, or a WaitAsync that raced disposal
            //never completes at all.
            lock (_asyncSync)
            {
                _asyncWait?.TrySetResult(false);
            }

            _waitHandle.Set();
            _waitHandle.Dispose();

            //Un-subscribe from the channel
            var sub = _connection.Connection.GetSubscriber();
            sub.UnsubscribeAsync(RedisChannel.Literal(_redisNames.Notification), Handler);
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        #endregion

        /// <summary>
        /// Setups this instance.
        /// </summary>
        private void Setup()
        {
            lock (_setup)
            {
                if (_ranSetup) return;

                _waitHandle = new ManualResetEventSlim(false);
                _waitHandle.Set();

                SubscribeForNotification();
                _ranSetup = true;
            }
        }
        /// <summary>
        /// Subscribes for notification.
        /// </summary>
        private void SubscribeForNotification()
        {
            var sub = _connection.Connection.GetSubscriber();
            sub.SubscribeAsync(RedisChannel.Literal(_redisNames.Notification), Handler);
        }

        /// <summary>
        /// Handlers the specified redis channel.
        /// </summary>
        /// <param name="redisChannel">The redis channel.</param>
        /// <param name="redisValue">The redis value.</param>
        private void Handler(RedisChannel redisChannel, RedisValue redisValue)
        {
            lock (_asyncSync)
            {
                //A notification can arrive from the Redis subscriber thread after disposal has begun.
                //Setting a disposed handle throws on a thread nobody is watching, so check inside the
                //same lock disposal uses rather than racing it.
                if (IsDisposed) return;

                _waitHandle.Set();
                _asyncWait?.TrySetResult(true);
            }
        }
    }
}
