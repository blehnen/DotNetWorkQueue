// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
    internal class GetHeaderLua: BaseLua
    {
        public GetHeaderLua(IRedisConnection connection, RedisNames redisNames) : base(connection, redisNames)
        {
            Script = @"local headers = redis.call('hget', @headerskey, @uuid) return headers";
        }
        /// <summary>
        /// Obtains the headers for a message
        /// </summary>
        /// <param name="messageId">The messageId.</param>
        /// <returns></returns>
        public byte[] Execute(string messageId)
        {
            if (Connection.IsDisposed)
                return null;

            var result = TryExecute(GetParameters(messageId));
            if (result.IsNull)
                return null;
            return (byte[]) result;
        }
        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="messageId">The messageId.</param>
        /// <returns></returns>
        private object GetParameters(string messageId)
        {
            return new
            {
                headerskey = (RedisKey)RedisNames.Headers,
                uuid = messageId
            };
        }
    }
}
