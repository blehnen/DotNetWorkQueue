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
namespace DotNetWorkQueue.Transport.Redis.Basic.Factory
{
    /// <summary>
    /// Creates new instance of <see cref="IRedisQueueWorkSub"/>
    /// </summary>
    internal class RedisQueueWorkSubFactory : IRedisQueueWorkSubFactory
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;
        private readonly IQueueCancelWork _cancelWork;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueWorkSubFactory" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        /// <param name="cancelWork">The cancel work.</param>
        public RedisQueueWorkSubFactory(IRedisConnection connection,
            RedisNames redisNames,
            IQueueCancelWork cancelWork)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);
            Guard.NotNull(() => cancelWork, cancelWork);

            _connection = connection;
            _redisNames = redisNames;
            _cancelWork = cancelWork;
        }
        /// <summary>
        /// Creates new instance of <see cref="IRedisQueueWorkSub" /> that will only respond if the specified ID is sent
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public IRedisQueueWorkSub Create(IMessageId id)
        {
            if (id == null || !id.HasValue)
            {
                return Create();
            }

            //return a new instance
            return new RedisQueueWorkSubRpc(_connection, _redisNames, _cancelWork, id);
        }

        /// <summary>
        /// Creates new instance of <see cref="IRedisQueueWorkSub" />
        /// </summary>
        /// <returns></returns>
        public IRedisQueueWorkSub Create()
        {
            return new RedisQueueWorkSub(_connection, _redisNames, _cancelWork);
        }
    }
}
