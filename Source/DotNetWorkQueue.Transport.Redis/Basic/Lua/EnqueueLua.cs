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
using System.Threading.Tasks;
using StackExchange.Redis;
namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <summary>
    /// Enqueues a message
    /// </summary>
    internal class EnqueueLua: BaseLua
    {
        public EnqueueLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"local signal = tonumber(@signalID)
                       local id = @field
                    
                     if id == '' then
                        id = redis.call('INCR', @IDKey)
                     end

                     redis.call('hset', @key, id, @value) 
                     redis.call('hset', @headerskey, id, @headers) 
                     redis.call('lpush', @pendingkey, id) 
                     redis.call('hset', @metakey, id, @metavalue) 
                     if signal == 1 then
                        redis.call('publish', @channel, id) 
                     else
                        redis.call('publish', @channel, '') 
                     end
                     return id";
        }
        /// <summary>
        /// Executes the specified message identifier.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="metaData">The meta data.</param>
        /// <param name="rpc">if set to <c>true</c> [RPC].</param>
        /// <returns></returns>
        public string Execute(string messageId, byte[] message, byte[] headers, byte[] metaData, bool rpc)
        {
            if (Connection.IsDisposed)
                return null;

            var db = Connection.Connection.GetDatabase();
            return (string)db.ScriptEvaluate(LoadedLuaScript, GetParameters(messageId, message, headers, metaData, rpc));
        }
        /// <summary>
        /// Executes the specified message identifier.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="metaData">The meta data.</param>
        /// <param name="rpc">if set to <c>true</c> [RPC].</param>
        /// <returns></returns>
        public async Task<string> ExecuteAsync(string messageId, byte[] message, byte[] headers, byte[] metaData, bool rpc)
        {
            var db = Connection.Connection.GetDatabase();
            return (string) await db.ScriptEvaluateAsync(LoadedLuaScript, GetParameters(messageId, message, headers, metaData, rpc)).ConfigureAwait(false);
        }
        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="metaData">The meta data.</param>
        /// <param name="rpc">if set to <c>true</c> [RPC].</param>
        /// <returns></returns>
        private object GetParameters(string messageId, byte[] message, byte[] headers, byte[] metaData, bool rpc)
        {
            return new
            {
                key = (RedisKey)RedisNames.Values,
                field = messageId,
                value = (RedisValue)message,
                headers,
                headerskey = (RedisKey)RedisNames.Headers,
                pendingkey = (RedisKey)RedisNames.Pending,
                channel = RedisNames.Notification,
                metakey = (RedisKey)RedisNames.MetaData,
                metavalue = (RedisValue)metaData,
                signalID = Convert.ToInt32(rpc),
                IDKey = (RedisKey)RedisNames.Id,
            };
        }
    }
}
