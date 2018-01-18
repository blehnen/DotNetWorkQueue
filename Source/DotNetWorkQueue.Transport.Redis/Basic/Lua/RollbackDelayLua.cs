using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <inheritdoc />
    /// <summary>
    /// Sends a message from the working queue into the delay queue
    /// </summary>
    internal class RollbackDelayLua : BaseLua
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackDelayLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public RollbackDelayLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"redis.call('zrem', @workingkey, @uuid) 
                       redis.call('zadd', @delaykey, @timestamp, @uuid) 
                       redis.call('hset', @StatusKey, @uuid, '0') 
                       return 1";
        }
        /// <summary>
        /// Sends a message from the working queue into the delay queue
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="unixTime">The unix time.</param>
        /// <returns></returns>
        public int? Execute(string messageId, long unixTime)
        {
            var db = Connection.Connection.GetDatabase();
            return (int) db.ScriptEvaluate(LoadedLuaScript, GetParameters(messageId, unixTime));
        }
        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="unixTime">The unix time.</param>
        /// <returns></returns>
        private object GetParameters(string messageId, long unixTime)
        {
            return new
            {
                workingkey = (RedisKey)RedisNames.Working,
                uuid = messageId,
                delaykey = (RedisKey)RedisNames.Delayed,
                timestamp = unixTime,
                StatusKey = (RedisKey)RedisNames.Status
            };
        }
    }
}
