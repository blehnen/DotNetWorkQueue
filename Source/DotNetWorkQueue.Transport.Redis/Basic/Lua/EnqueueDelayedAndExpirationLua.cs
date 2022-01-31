﻿// ---------------------------------------------------------------------
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
using System.Threading.Tasks;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <inheritdoc />
    /// <summary>
    /// Enqueues a message that is both delayed and has expiration
    /// </summary>
    internal class EnqueueDelayedAndExpirationLua : BaseLua
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="EnqueueDelayedAndExpirationLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public EnqueueDelayedAndExpirationLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"local id = @field
                    
                     if id == '' then
                        id = redis.call('INCR', @IDKey)
                     end

                     redis.call('hset', @key, id, @value) 
                     redis.call('hset', @headerskey, id, @headers) 
                     redis.call('zadd', @delaykey, @timestamp, id) 
                     redis.call('zadd', @expirekey, @timestampexpire, id) 
                     redis.call('hset', @metakey, id, @metavalue) 
                     redis.call('hset', @StatusKey, id, '0') 

                     if @Route ~= '' then
                         redis.call('hset', @RouteIDKey, id, @Route)
                     end
                     return id";
        }
        /// <summary>
        /// Enqueues a message that is both delayed and has expiration
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="metaData">The meta data.</param>
        /// <param name="delayTime">The delay time.</param>
        /// <param name="expireTime">The expire time.</param>
        /// <param name="route">The route.</param>
        /// <returns></returns>
        public string Execute(string messageId, byte[] message, byte[] headers,
            byte[] metaData, long delayTime, long expireTime, string route)
        {
            if (Connection.IsDisposed)
                return null;

            var result = TryExecute(GetParameters(messageId, message, headers, metaData, delayTime, expireTime, route));
            if (result.IsNull)
                return null;
            return (string)result;
        }
        /// <summary>
        /// Enqueues a message that is both delayed and has expiration
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="metaData">The meta data.</param>
        /// <param name="delayTime">The delay time.</param>
        /// <param name="expireTime">The expire time.</param>
        /// <param name="route">The route.</param>
        /// <returns></returns>
        public async Task<string> ExecuteAsync(string messageId, byte[] message, byte[] headers, byte[] metaData,
            long delayTime, long expireTime, string route)
        {
            var result = await TryExecuteAsync(GetParameters(messageId, message, headers, metaData, delayTime, expireTime, route)).ConfigureAwait(false);
            if (result.IsNull)
                return null;
            return (string)result;
        }
        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="metaData">The meta data.</param>
        /// <param name="delayTime">The delay time.</param>
        /// <param name="expireTime">The expire time.</param>
        /// <param name="route">The route.</param>
        /// <returns></returns>
        private object GetParameters(string messageId, byte[] message, byte[] headers, byte[] metaData,
            long delayTime, long expireTime, string route)
        {
            var realRoute = string.IsNullOrEmpty(route) ? string.Empty : route;
            return
            new
            {
                key = (RedisKey)RedisNames.Values,
                field = messageId,
                value = message,
                delaykey = (RedisKey)RedisNames.Delayed,
                channel = RedisNames.Notification,
                headers,
                Route = realRoute,
                headerskey = (RedisKey)RedisNames.Headers,
                metakey = (RedisKey)RedisNames.MetaData,
                metavalue = metaData,
                expirekey = (RedisKey)RedisNames.Expiration,
                timestamp = delayTime,
                timestampexpire = expireTime,
                IDKey = (RedisKey)RedisNames.Id,
                StatusKey = (RedisKey)RedisNames.Status,
                RouteIDKey = (RedisKey)RedisNames.Route
            };
        }
    }
}
