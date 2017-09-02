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

using System.Collections.Generic;
using System.Threading;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <inheritdoc />
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

        /// <inheritdoc />
        public IConnectionInformation ConnectionInfo { get; }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public ICreationScope Scope { get; }

        /// <inheritdoc />
        /// <remarks>This does nothing for the Redis transport, as pre-creating the queue is not necessary.</remarks>
        /// <returns></returns>
        public QueueCreationResult CreateQueue()
        {
            return new QueueCreationResult(QueueCreationStatus.NoOp);
        }

        /// <inheritdoc />
        public QueueRemoveResult RemoveQueue()
        {
            return QueueExists ? RemoveQueueInternal() : new QueueRemoveResult(QueueRemoveStatus.DoesNotExist);
        }

        #region IDisposable, IsDisposed
        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.Increment(ref _disposeCount) == 1)
            {
               
            }
        }

        /// <inheritdoc />
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
