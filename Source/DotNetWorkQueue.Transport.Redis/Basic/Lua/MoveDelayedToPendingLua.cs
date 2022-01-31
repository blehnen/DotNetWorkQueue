// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <inheritdoc />
    /// <summary>
    /// Moves delayed records to the pending queue
    /// </summary>
    internal class MoveDelayedToPendingLua : BaseLua
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="MoveDelayedToPendingLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public MoveDelayedToPendingLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"local uuids = redis.call('zrangebyscore', @delaykey, 0, @currenttime, 'LIMIT', 0, @limit)
                       if #uuids == 0 then
                        return 0
                       end

                       for k, v in pairs(uuids) do                             
                        redis.call('zrem',  @delaykey, v)
                        
                        local routeName = redis.call('hget', @RouteIDKey, v) 
                        if(routeName) then
                            local routePending = @pendingkey .. '_}' .. routeName
                            redis.call('lpush', routePending, v) 
                        else
                            redis.call('lpush', @pendingkey, v) 
                        end
                       end
                      redis.call('publish', @channel, '') 
                      return table.getn(uuids)";
        }
        /// <summary>
        /// Moves delayed records to the pending queue
        /// </summary>
        /// <param name="unixTime">The unix time.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public int Execute(long unixTime, int count)
        {
            if (Connection.IsDisposed)
                return 0;

            var result = TryExecute(GetParameters(unixTime, count));
            if (result.IsNull)
                return 0;
            return (int)result;
        }
        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="unixTime">The unix time.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        private object GetParameters(long unixTime, int count)
        {
            return new
            {
                pendingkey = (RedisKey)RedisNames.Pending,
                delaykey = (RedisKey)RedisNames.Delayed,
                currenttime = unixTime,
                channel = RedisNames.Notification,
                limit = count,
                RouteIDKey = (RedisKey)RedisNames.Route
            };
        }
    }
}
