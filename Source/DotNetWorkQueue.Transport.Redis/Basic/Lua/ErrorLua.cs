using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <inheritdoc />
    /// <summary>
    /// Moves a message from the working queue to the error queue
    /// </summary>
    internal class ErrorLua: BaseLua
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public ErrorLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"redis.call('zrem', @workingkey, @uuid) 
                     redis.call('lpush', @errorkey, @uuid) 
                     redis.call('zadd', @errortimekey, @timestamp, @uuid) 
                     redis.call('hset', @StatusKey, @uuid, '2') ";
        }
        /// <summary>
        /// Executes the specified message identifier.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="unixTime">current time</param>
        /// <returns></returns>
        public int? Execute(string messageId, long unixTime)
        {
            var db = Connection.Connection.GetDatabase();
            return (int) db.ScriptEvaluate(LoadedLuaScript, GetParameters(messageId, unixTime));
        }
        /// <summary>Gets the parameters.</summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="unixTime">current time</param>
        /// <returns></returns>
        private object GetParameters(string messageId, long unixTime)
        {
            return new
            {
                workingkey = (RedisKey)RedisNames.Working,
                uuid = messageId,
                errortimekey = (RedisKey)RedisNames.ErrorTime,
                errorkey = (RedisKey)RedisNames.Error,
                StatusKey = (RedisKey)RedisNames.Status,
                timestamp = unixTime
            };
        }
    }
}
