// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Allows a caller to wait and block until a pub/sub call has occured for a specific message id
    /// </summary>
    public class RedisQueueWorkSubRpc : IRedisQueueWorkSub
    {
        #region Member Level Variables
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;
        private readonly ManualResetEventSlim _waitHandle;
        private readonly ICancelWork _cancelWork;
        private readonly IMessageId _messageId;

        private readonly object _locker = new object();
        private readonly object _setup = new object();
        private bool _ranSetup;

        private int _disposeCount;

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueWorkSubRpc"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        /// <param name="cancelWork">The cancel work.</param>
        /// <param name="messageId">The message identifier.</param>
        public RedisQueueWorkSubRpc(IRedisConnection connection,
            RedisNames redisNames,
            IQueueCancelWork cancelWork,
            IMessageId messageId)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);
            Guard.NotNull(() => cancelWork, cancelWork);

            _connection = connection;
            _redisNames = redisNames;
            _cancelWork = cancelWork;
            _messageId = messageId;

            _waitHandle = new ManualResetEventSlim(false);
            _waitHandle.Set();
        }
        #endregion

        /// <summary>
        /// Waits until a notification is received, or until the timeout is reached
        /// </summary>
        /// <param name="timeout">The timeout. Null means no timeout.</param>
        /// <returns></returns>
        public bool Wait(TimeSpan? timeout)
        {
            ThrowIfDisposed();

            Setup();
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancelWork.Tokens.ToArray()))
            {
                //no lock here - this is on purpose
                try
                {
                    if (timeout.HasValue)
                    {
                        var smaller = TimeSpan.FromMilliseconds(timeout.Value.TotalMilliseconds / 4);
                        // ReSharper disable once InconsistentlySynchronizedField
                        return _waitHandle.Wait(smaller, cts.Token);
                    }

                    // ReSharper disable once InconsistentlySynchronizedField
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
        public void Reset()
        {
            Setup();
            lock (_locker)
            {
                if (_waitHandle.IsSet)
                    _waitHandle.Reset();
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

            lock (_locker)
            {
                _waitHandle.Set();
                _waitHandle.Dispose();
            }

            if (!_connection.IsDisposed)
            {
                var sub = _connection.Connection.GetSubscriber();
                sub.UnsubscribeAsync(_redisNames.Notification, Handler);
            }
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
            if (_ranSetup) return;
            lock (_setup)
            {
                if (_ranSetup) return;
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
            sub.SubscribeAsync(_redisNames.Notification, Handler);
        }

        /// <summary>
        /// Handlers the specified redis channel.
        /// </summary>
        /// <param name="redisChannel">The redis channel.</param>
        /// <param name="redisValue">The redis value.</param>
        private void Handler(RedisChannel redisChannel, RedisValue redisValue)
        {
            if (!redisValue.HasValue || _messageId.Id.Value.ToString() != redisValue.ToString())
            {
                return;
            }

            lock (_locker)
            {
                _waitHandle.Set();
            }
        }
    }
}
