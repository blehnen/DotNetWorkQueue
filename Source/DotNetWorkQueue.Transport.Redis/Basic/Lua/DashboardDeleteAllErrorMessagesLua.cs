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
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <summary>
    /// Atomically deletes all error messages and their associated data
    /// </summary>
    internal class DashboardDeleteAllErrorMessagesLua : BaseLua
    {
        /// <inheritdoc />
        public DashboardDeleteAllErrorMessagesLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"local ids = redis.call('LRANGE', @errorkey, 0, -1)
                       if #ids == 0 then
                           return 0
                       end
                       for i, id in ipairs(ids) do
                           redis.call('hdel', @metakey, id)
                           redis.call('hdel', @valueskey, id)
                           redis.call('hdel', @headerskey, id)
                           redis.call('hdel', @StatusKey, id)
                           redis.call('zrem', @errortimekey, id)
                       end
                       redis.call('del', @errorkey)
                       return #ids";
        }

        /// <summary>
        /// Deletes all error messages atomically.
        /// </summary>
        /// <returns>The number of messages deleted.</returns>
        public int Execute()
        {
            var result = TryExecute(GetParameters());
            if (result.IsNull) return 0;
            return (int)result;
        }

        private object GetParameters()
        {
            return new
            {
                errorkey = (RedisKey)RedisNames.Error,
                metakey = (RedisKey)RedisNames.MetaData,
                valueskey = (RedisKey)RedisNames.Values,
                headerskey = (RedisKey)RedisNames.Headers,
                StatusKey = (RedisKey)RedisNames.Status,
                errortimekey = (RedisKey)RedisNames.ErrorTime
            };
        }
    }
}
