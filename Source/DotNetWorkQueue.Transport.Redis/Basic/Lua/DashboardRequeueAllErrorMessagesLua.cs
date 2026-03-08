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
    /// Atomically requeues all error messages back to pending (Waiting) status
    /// </summary>
    internal class DashboardRequeueAllErrorMessagesLua : BaseLua
    {
        /// <inheritdoc />
        public DashboardRequeueAllErrorMessagesLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"local ids = redis.call('LRANGE', @errorkey, 0, -1)
                       if #ids == 0 then
                           return 0
                       end
                       for i, id in ipairs(ids) do
                           redis.call('zrem', @errortimekey, id)
                           redis.call('hset', @StatusKey, id, '0')
                           local metaBytes = redis.call('hget', @MetaDataKey, id)
                           if metaBytes then
                               local ok, meta = pcall(cjson.decode, metaBytes)
                               if ok and meta and meta['ErrorTracking'] and meta['ErrorTracking']['Errors'] then
                                   meta['ErrorTracking']['Errors'] = {}
                                   redis.call('hset', @MetaDataKey, id, cjson.encode(meta))
                               end
                           end
                           local routeName = redis.call('hget', @RouteIDKey, id)
                           if(routeName) then
                               local routePending = @pendingkey .. '_}' .. routeName
                               redis.call('rpush', routePending, id)
                           else
                               redis.call('rpush', @pendingkey, id)
                           end
                       end
                       redis.call('del', @errorkey)
                       redis.call('publish', @channel, '')
                       return #ids";
        }

        /// <summary>
        /// Requeues all error messages atomically.
        /// </summary>
        /// <returns>The number of messages requeued.</returns>
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
                errortimekey = (RedisKey)RedisNames.ErrorTime,
                StatusKey = (RedisKey)RedisNames.Status,
                MetaDataKey = (RedisKey)RedisNames.MetaData,
                pendingkey = (RedisKey)RedisNames.Pending,
                channel = RedisNames.Notification,
                RouteIDKey = (RedisKey)RedisNames.Route
            };
        }
    }
}
