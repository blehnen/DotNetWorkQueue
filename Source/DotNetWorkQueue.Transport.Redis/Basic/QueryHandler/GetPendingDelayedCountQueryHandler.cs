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

using DotNetWorkQueue.Transport.Redis.Basic.Query;
namespace DotNetWorkQueue.Transport.Redis.Basic.QueryHandler
{
    /// <summary>
    /// Returns the current delayed count
    /// </summary>
    internal class GetPendingDelayedCountQueryHandler : IQueryHandler<GetPendingDelayedCountQuery, long>
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetPendingDelayedCountQueryHandler" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public GetPendingDelayedCountQueryHandler(
            IRedisConnection connection,
            RedisNames redisNames)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);

            _connection = connection;
            _redisNames = redisNames;
        }

        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public long Handle(GetPendingDelayedCountQuery query)
        {
            var db = _connection.Connection.GetDatabase();
            return db.SortedSetLength(_redisNames.Delayed);
        }
    }
}
