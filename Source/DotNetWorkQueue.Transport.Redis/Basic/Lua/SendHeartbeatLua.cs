using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <inheritdoc />
    /// <summary>
    /// Sends a heartbeat for a single message
    /// </summary>
    /// <remarks>client does not support options of 'zadd'; we are using LUA script to work around this</remarks>
    internal class SendHeartbeatLua: BaseLua
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="SendHeartbeatLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public SendHeartbeatLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"redis.call('zadd', @workingkey, 'XX', @timestamp, @uuid) ";
        }
        /// <summary>
        /// Sends a heartbeat for a single message
        /// </summary>
        /// <param name="messageId">The messageId.</param>
        /// <param name="unixTime">The unix time.</param>
        /// <returns></returns>
        public int? Execute(string messageId, long unixTime)
        {
            if (Connection.IsDisposed)
                return null;

            var db = Connection.Connection.GetDatabase();
            return (int)db.ScriptEvaluate(LoadedLuaScript, GetParameters(messageId, unixTime));
        }
        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="messageId">The messageId.</param>
        /// <param name="unixTime">The unix time.</param>
        /// <returns></returns>
        private object GetParameters(string messageId, long unixTime)
        {
            return new
            {
                workingkey = (RedisKey)RedisNames.Working,
                timestamp = unixTime,
                uuid = messageId
            };
        }

    }
}
