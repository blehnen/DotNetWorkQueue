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
using System;
using StackExchange.Redis;
namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <summary>
    /// Moves a message from the working queue back into the pending queue
    /// </summary>
    internal class RollbackLua : BaseLua
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public RollbackLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"local signal = tonumber(@signalID)
                     redis.call('zrem', @workingkey, @uuid) 
                     local routeName = redis.call('hget', @RouteIDKey, @uuid) 
                     if(routeName) then
                        local routePending = @pendingkey .. '_}' .. routeName
                        redis.call('rpush', routePending, @uuid) 
                     else
                        redis.call('rpush', @pendingkey, @uuid)
                     end
                     redis.call('hset', @StatusKey, @uuid, '0') 
                     if signal == 1 then
                       redis.call('publish', @channel, @uuid) 
                     else
                        redis.call('publish', @channel, '') 
                     end
                     return 1";
        }
        /// <summary>
        /// Moves a message from the working queue back into the pending queue
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="rpc">if set to <c>true</c> [RPC].</param>
        /// <returns></returns>
        public int? Execute(string messageId, bool rpc)
        {
            var db = Connection.Connection.GetDatabase();
            return (int) db.ScriptEvaluate(LoadedLuaScript, GetParameters(messageId, rpc));
        }
        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="rpc">if set to <c>true</c> [RPC].</param>
        /// <returns></returns>
        private object GetParameters(string messageId, bool rpc)
        {
            return new
            {
                workingkey = (RedisKey)RedisNames.Working,
                uuid = messageId,
                pendingkey = (RedisKey)RedisNames.Pending,
                channel = RedisNames.Notification,
                signalID = Convert.ToInt32(rpc),
                RouteIDKey = (RedisKey)RedisNames.Route,
                StatusKey = (RedisKey)RedisNames.Status,
            };
        }
    }
}
