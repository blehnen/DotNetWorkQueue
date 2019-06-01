using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <inheritdoc />
    /// <summary>
    /// Enqueues a message with expiration
    /// </summary>
    internal class EnqueueExpirationLua : BaseLua
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="EnqueueExpirationLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public EnqueueExpirationLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"local id = @field
                    
                     if id == '' then
                        id = redis.call('INCR', @IDKey)
                     end

                     redis.call('hset', @key, id, @value) 
                     redis.call('hset', @headerskey, id, @headers) 
                     redis.call('lpush', @pendingkey, id) 
                     redis.call('zadd', @expirekey, @timestampexpire, id) 
                     redis.call('hset', @metakey, id, @metavalue) 
                     redis.call('hset', @StatusKey, id, '0') 
                      if @Route ~= '' then
                         redis.call('hset', @RouteIDKey, id, @Route)
                     end
                     redis.call('publish', @channel, '') 
                     return id";
        }
        /// <summary>
        /// Enqueues a message with an expiration
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="metaData">The meta data.</param>
        /// <param name="expireTime">The expire time.</param>
        /// <param name="route">The route.</param>
        /// <returns></returns>
        public string Execute(string messageId, byte[] message, byte[] headers, byte[] metaData, long expireTime, string route)
        {
            if (Connection.IsDisposed)
                return null;

            var db = Connection.Connection.GetDatabase();
            return (string)db.ScriptEvaluate(LoadedLuaScript, GetParameters(messageId, message, headers, metaData, expireTime, route));
        }
        /// <summary>
        /// Enqueues a message with an expiration
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="metaData">The meta data.</param>
        /// <param name="expireTime">The expire time.</param>
        /// <param name="route">The route.</param>
        /// <returns></returns>
        public async Task<string> ExecuteAsync(string messageId, byte[] message, byte[] headers, byte[] metaData, long expireTime, string route)
        {
            var db = Connection.Connection.GetDatabase();
            return (string) await db.ScriptEvaluateAsync(LoadedLuaScript, GetParameters(messageId, message, headers, metaData, expireTime, route)).ConfigureAwait(false);
        }
        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="metaData">The meta data.</param>
        /// <param name="expireTime">The expire time.</param>
        /// <param name="route">The route.</param>
        /// <returns></returns>
        private object GetParameters(string messageId, byte[] message, byte[] headers, byte[] metaData, 
            long expireTime, string route)
        {
            var pendingKey = !string.IsNullOrEmpty(route) ? RedisNames.PendingRoute(route) : RedisNames.Pending;
            var realRoute = string.IsNullOrEmpty(route) ? string.Empty : route;
            return
            new
            {
                key = (RedisKey)RedisNames.Values,
                field = messageId,
                value = message,
                pendingkey = (RedisKey)pendingKey,
                channel = RedisNames.Notification,
                headers,
                Route = realRoute,
                headerskey = (RedisKey)RedisNames.Headers,
                metakey = (RedisKey)RedisNames.MetaData,
                metavalue = metaData,
                expirekey = (RedisKey)RedisNames.Expiration,
                timestampexpire = expireTime,
                IDKey = (RedisKey)RedisNames.Id,
                StatusKey = (RedisKey)RedisNames.Status,
                RouteIDKey = (RedisKey)RedisNames.Route
            };
        }
    }
}
