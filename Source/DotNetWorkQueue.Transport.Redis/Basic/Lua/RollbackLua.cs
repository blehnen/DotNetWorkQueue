using System;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <inheritdoc />
    /// <summary>
    /// Moves a message from the working queue back into the pending queue
    /// </summary>
    internal class RollbackLua : BaseLua
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public RollbackLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"redis.call('zrem', @workingkey, @uuid) 
                     local routeName = redis.call('hget', @RouteIDKey, @uuid) 
                     if(routeName) then
                        local routePending = @pendingkey .. '_}' .. routeName
                        redis.call('rpush', routePending, @uuid) 
                     else
                        redis.call('rpush', @pendingkey, @uuid)
                     end
                     redis.call('hset', @StatusKey, @uuid, '0') 
                     redis.call('publish', @channel, '') 
                     return 1";
        }
        /// <summary>
        /// Moves a message from the working queue back into the pending queue
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <returns></returns>
        public int? Execute(string messageId)
        {
            var db = Connection.Connection.GetDatabase();
            return (int) db.ScriptEvaluate(LoadedLuaScript, GetParameters(messageId));
        }
        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <returns></returns>
        private object GetParameters(string messageId)
        {
            return new
            {
                workingkey = (RedisKey)RedisNames.Working,
                uuid = messageId,
                pendingkey = (RedisKey)RedisNames.Pending,
                channel = RedisNames.Notification,
                RouteIDKey = (RedisKey)RedisNames.Route,
                StatusKey = (RedisKey)RedisNames.Status
            };
        }
    }
}
