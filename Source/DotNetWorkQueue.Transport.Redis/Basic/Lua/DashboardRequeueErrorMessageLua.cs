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
    /// Moves an error message back to pending (Waiting) status
    /// </summary>
    internal class DashboardRequeueErrorMessageLua : BaseLua
    {
        /// <inheritdoc />
        public DashboardRequeueErrorMessageLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"local removed = redis.call('LREM', @errorkey, -1, @uuid)
                       if removed == 0 then
                           return 0
                       end
                       redis.call('zrem', @errortimekey, @uuid)
                       redis.call('hset', @StatusKey, @uuid, '0')
                       local routeName = redis.call('hget', @RouteIDKey, @uuid)
                       if(routeName) then
                           local routePending = @pendingkey .. '_}' .. routeName
                           redis.call('rpush', routePending, @uuid)
                       else
                           redis.call('rpush', @pendingkey, @uuid)
                       end
                       redis.call('publish', @channel, '')
                       return 1";
        }

        /// <summary>
        /// Moves a message from the error list back to pending.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <returns>1 if the message was moved, 0 if not found.</returns>
        public int Execute(string messageId)
        {
            var result = TryExecute(GetParameters(messageId));
            if (result.IsNull) return 0;
            return (int)result;
        }

        private object GetParameters(string messageId)
        {
            return new
            {
                errorkey = (RedisKey)RedisNames.Error,
                errortimekey = (RedisKey)RedisNames.ErrorTime,
                uuid = messageId,
                StatusKey = (RedisKey)RedisNames.Status,
                pendingkey = (RedisKey)RedisNames.Pending,
                channel = RedisNames.Notification,
                RouteIDKey = (RedisKey)RedisNames.Route
            };
        }
    }
}
