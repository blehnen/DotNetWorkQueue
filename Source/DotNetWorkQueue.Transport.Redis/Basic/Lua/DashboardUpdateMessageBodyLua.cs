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
    /// Atomically verifies a message exists and updates its body and headers
    /// </summary>
    internal class DashboardUpdateMessageBodyLua : BaseLua
    {
        /// <inheritdoc />
        public DashboardUpdateMessageBodyLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"if redis.call('HEXISTS', @valueskey, @uuid) == 0 then
                           return 0
                       end
                       redis.call('HSET', @valueskey, @uuid, @body)
                       redis.call('HSET', @headerskey, @uuid, @headers)
                       return 1";
        }

        /// <summary>
        /// Updates the body and headers of a message atomically.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="body">The new body bytes.</param>
        /// <param name="headers">The new header bytes.</param>
        /// <returns>1 if the message was updated, 0 if not found.</returns>
        public int Execute(string messageId, byte[] body, byte[] headers)
        {
            var result = TryExecute(GetParameters(messageId, body, headers));
            if (result.IsNull) return 0;
            return (int)result;
        }

        private object GetParameters(string messageId, byte[] body, byte[] headers)
        {
            return new
            {
                valueskey = (RedisKey)RedisNames.Values,
                headerskey = (RedisKey)RedisNames.Headers,
                uuid = messageId,
                body,
                headers
            };
        }
    }
}
