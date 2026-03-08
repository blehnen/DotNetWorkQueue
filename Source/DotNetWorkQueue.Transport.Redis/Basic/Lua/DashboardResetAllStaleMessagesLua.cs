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
    /// Atomically resets all stale (working/processing) messages back to pending
    /// </summary>
    internal class DashboardResetAllStaleMessagesLua : BaseLua
    {
        /// <inheritdoc />
        public DashboardResetAllStaleMessagesLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"local ids = redis.call('ZRANGEBYSCORE', @workingkey, '-inf', '+inf')
                       if #ids == 0 then
                           return 0
                       end
                       for i, id in ipairs(ids) do
                           redis.call('zrem', @workingkey, id)
                           redis.call('hset', @StatusKey, id, '0')
                           local routeName = redis.call('hget', @RouteIDKey, id)
                           if(routeName) then
                               local routePending = @pendingkey .. '_}' .. routeName
                               redis.call('rpush', routePending, id)
                           else
                               redis.call('rpush', @pendingkey, id)
                           end
                       end
                       redis.call('publish', @channel, '')
                       return #ids";
        }

        /// <summary>
        /// Resets all stale working messages atomically.
        /// </summary>
        /// <returns>The number of messages reset.</returns>
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
                workingkey = (RedisKey)RedisNames.Working,
                StatusKey = (RedisKey)RedisNames.Status,
                pendingkey = (RedisKey)RedisNames.Pending,
                channel = RedisNames.Notification,
                RouteIDKey = (RedisKey)RedisNames.Route
            };
        }
    }
}
