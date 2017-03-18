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
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.QueryHandler
{
    /// <summary>
    /// Returns a records meta data from the transport
    /// </summary>
    internal class GetMetaDataQueryHandler : IQueryHandler<GetMetaDataQuery, RedisMetaData>
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;
        private readonly IInternalSerializer _internalSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageQueryHandler" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        /// <param name="internalSerializer">The internal serializer.</param>
        public GetMetaDataQueryHandler(
            IRedisConnection connection,
            RedisNames redisNames, 
            IInternalSerializer internalSerializer)
        {
            Guard.NotNull(() => internalSerializer, internalSerializer);
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);

            _connection = connection;
            _redisNames = redisNames;
            _internalSerializer = internalSerializer;
        }

        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public RedisMetaData Handle(GetMetaDataQuery query)
        {
            var db = _connection.Connection.GetDatabase();
            var result = (byte[])db.HashGet(_redisNames.MetaData, query.Id.Id.Value.ToString());
            if (result != null && result.Length > 0)
            {
                return _internalSerializer.ConvertBytesTo<RedisMetaData>(result);
            }
            return null;
        }
    }
}
