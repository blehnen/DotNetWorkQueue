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
using System;
using StackExchange.Redis;
namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <summary>
    /// Resets the heartbeat for records outside of the window.
    /// </summary>
    internal class ResetHeartbeatLua : BaseLua
    {
        public ResetHeartbeatLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"local signal = tonumber(@signalID)
                        local uuids = redis.call('zrangebyscore', @workingkey, 0, @heartbeattime, 'LIMIT', 0, @limit)
                        if #uuids == 0 then
	                        return 0
                        end
                        for k, v in pairs(uuids) do                             
	                        redis.call('zrem',  @workingkey, v)
                            local routeName = redis.call('hget', @RouteIDKey, v) 
                            if(routeName) then
                                local routePending = @pendingkey .. '_}' .. routeName
                                redis.call('rpush', routePending, v) 
                            else
	                            redis.call('rpush', @pendingkey, v) 
                            end
                            redis.call('hset', @StatusKey, v, '0') 
	                        if signal == 1 then
		                        redis.call('publish', @channel, v) 
	                        end
                        end

                        if signal == 0 then
	                        redis.call('publish', @channel, '') 
                        end
                        return table.getn(uuids)";
        }
        /// <summary>
        /// Resets the heartbeat for records outside of the window.
        /// </summary>
        /// <param name="unixTime">The unix time.</param>
        /// <param name="count">The count.</param>
        /// <param name="rpc">if set to <c>true</c> [RPC].</param>
        /// <returns></returns>
        public int Execute(long unixTime, int count, bool rpc)
        {
            if (Connection.IsDisposed)
                return 0;

            var db = Connection.Connection.GetDatabase();
            return (int) db.ScriptEvaluate(LoadedLuaScript, GetParameters(unixTime, count, rpc));
        }
        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="heartbeatResetTime">The heartbeat reset time.</param>
        /// <param name="count">The count.</param>
        /// <param name="rpc">if set to <c>true</c> [RPC].</param>
        /// <returns></returns>
        private object GetParameters(long heartbeatResetTime, int count, bool rpc)
        {
            return new
            {
                pendingkey = (RedisKey)RedisNames.Pending,
                workingkey = (RedisKey)RedisNames.Working,
                heartbeattime = heartbeatResetTime,
                channel = RedisNames.Notification,
                limit = count,
                signalID = Convert.ToInt32(rpc),
                RouteIDKey = (RedisKey)RedisNames.Route,
                StatusKey = (RedisKey)RedisNames.Status,
            };
        }
    }
}
