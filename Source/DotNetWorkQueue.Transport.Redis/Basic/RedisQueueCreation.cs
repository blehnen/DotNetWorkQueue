﻿// ---------------------------------------------------------------------
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
using System.Collections.Generic;
using System.Threading;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Allows deleting redis queues; creation is a no-op, as redis queues do not need to be pre-created.
    /// </summary>
    public sealed class RedisQueueCreation : IQueueCreation
    {
        private readonly RedisNames _redisNames;
        private readonly IRedisConnection _redisConnection;
        private int _disposeCount;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueCreation" /> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="redisConnection">The redis connection.</param>
        /// <param name="redisNames">The redis names.</param>
        /// <param name="creationScope">The creation scope.</param>
        public RedisQueueCreation(IConnectionInformation connectionInfo,
            IRedisConnection redisConnection,
            RedisNames redisNames,
            ICreationScope creationScope)
        {
            Guard.NotNull(() => connectionInfo, connectionInfo);
            Guard.NotNull(() => redisConnection, redisConnection);
            Guard.NotNull(() => redisNames, redisNames);
            Guard.NotNull(() => creationScope, creationScope);

            _redisConnection = redisConnection;
            _redisNames = redisNames;
            ConnectionInfo = connectionInfo;
            Scope = creationScope;
        }

        #endregion

        /// <summary>
        /// Gets the connection information for the queue.
        /// </summary>
        /// <value>
        /// The connection information.
        /// </value>
        public IConnectionInformation ConnectionInfo { get; }

        /// <summary>
        /// Returns true if the queue exists in the transport
        /// </summary>
        /// <value>
        ///   <c>true</c> if [queue exists]; otherwise, <c>false</c>.
        /// </value>
        public bool QueueExists
        {
            get
            {
                var db = _redisConnection.Connection.GetDatabase();
                return db.KeyExists(_redisNames.Delayed) ||
                       db.KeyExists(_redisNames.Error) ||
                       db.KeyExists(_redisNames.Expiration) ||
                       db.HashLength(_redisNames.MetaData) > 0 ||
                       db.KeyExists(_redisNames.Pending) ||
                       db.HashLength(_redisNames.Values) > 0 ||
                       db.SetLength(_redisNames.Working) > 0 ||
                       db.KeyExists(_redisNames.Id) ||
                       db.HashLength(_redisNames.Headers) > 0;
            }
        }

        /// <summary>
        /// Gets a disposable creation scope
        /// </summary>
        /// <value>
        /// The scope.
        /// </value>
        /// <remarks>This is used to prevent queues from going out of scope before you have finished working with them. Generally
        /// speaking this only matters for queues that live in-memory. However, a valid object is always returned.</remarks>
        public ICreationScope Scope { get; }

        /// <summary>
        /// Creates the queue if needed.
        /// </summary>
        /// <remarks>This does nothing for the Redis transport, as pre-creating the queue is not necessary.</remarks>
        /// <returns></returns>
        public QueueCreationResult CreateQueue()
        {
            return new QueueCreationResult(QueueCreationStatus.NoOp);
        }

        /// <summary>
        /// Attempts to delete an existing queue
        /// </summary>
        /// <remarks>Any data in the queue will be lost. Will cause exceptions in any producer/consumer that is connected</remarks>
        /// <returns></returns>
        public QueueRemoveResult RemoveQueue()
        {
            return QueueExists ? RemoveQueueInternal() : new QueueRemoveResult(QueueRemoveStatus.DoesNotExist);
        }

        #region IDisposable, IsDisposed
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Increment(ref _disposeCount) == 1)
            {
               
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
        /// Deletes a queue by deleting all of its keys
        /// </summary>
        /// <returns></returns>
        private QueueRemoveResult RemoveQueueInternal()
        {
            var db = _redisConnection.Connection.GetDatabase();
            var routesKey = _redisNames.Route;
            foreach (var key in _redisNames.KeyNames)
            {
                if (key == routesKey)
                { //delete pending route items
                    var hashset = new HashSet<string>();
                    var records = db.HashGetAll(routesKey);
                    foreach (var routeName in records)
                    {
                        if (hashset.Contains(routeName.Value)) continue;
                        var keyToDelete = _redisNames.PendingRoute(routeName.Value);
                        db.KeyDelete(keyToDelete);
                        hashset.Add(routeName.Value);
                    }
                }
                db.KeyDelete(key);
            }
            return new QueueRemoveResult(QueueRemoveStatus.Success);
        }
    }
}
