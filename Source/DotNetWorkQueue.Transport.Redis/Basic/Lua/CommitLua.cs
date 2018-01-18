using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <inheritdoc />
    internal class CommitLua:BaseLua
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public CommitLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"redis.call('zrem', @workingkey, @uuid) 
                     redis.call('hdel', @valueskey, @uuid) 
                     redis.call('hdel', @headerskey, @uuid) 
                     redis.call('hdel', @metakey, @uuid) 
                     redis.call('LREM', @errorkey, -1, @uuid) 
                     redis.call('zrem', @expirekey, @uuid) 
                     redis.call('hdel', @StatusKey, @uuid) 

                     local jobName = redis.call('hget', @JobIDKey, @uuid) 
                     if (jobName) then
                        redis.call('hdel', @JobIDKey, @uuid) 
                        redis.call('hdel', @JobKey, jobName) 
                     end
                    
                     local routeName = redis.call('hget', @RouteIDKey, @uuid) 
                     if(routeName) then
                         redis.call('hdel', @RouteIDKey, @uuid) 
                     end
                     return 1";
        }
        /// <summary>
        /// Commits the message by removing it from the queue.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <returns></returns>
        public int? Execute(string messageId)
        {
            var db = Connection.Connection.GetDatabase();
            return (int)db.ScriptEvaluate(LoadedLuaScript, GetParameters(messageId));
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
                valueskey = (RedisKey)RedisNames.Values,
                headerskey = (RedisKey)RedisNames.Headers,
                metakey = (RedisKey)RedisNames.MetaData,
                errorkey = (RedisKey)RedisNames.Error,
                expirekey = (RedisKey)RedisNames.Expiration,
                JobKey = (RedisKey)RedisNames.JobNames,
                JobIDKey = (RedisKey)RedisNames.JobIdNames,
                StatusKey = (RedisKey)RedisNames.Status,
                RouteIDKey = (RedisKey)RedisNames.Route
            };
        }
    }
}
