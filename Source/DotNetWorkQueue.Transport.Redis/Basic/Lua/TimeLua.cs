using System;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <inheritdoc />
    /// <summary>
    /// Gets the current time from a redis server
    /// </summary>
    internal class TimeLua: BaseLua
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public TimeLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"return redis.call('time')";
        }
        /// <summary>
        /// Gets the current time from a redis server
        /// </summary>
        /// <returns></returns>
        public long Execute()
        {
            var db = Connection.Connection.GetDatabase();
            var result = (RedisValue[])db.ScriptEvaluate(LoadedLuaScript);
            var seconds = (long) result[0];
            var milliseconds = (long) result[1]/1000; //convert microseconds to milliseconds
            return (long)TimeSpan.FromSeconds(seconds).Add(TimeSpan.FromMilliseconds(milliseconds)).TotalMilliseconds;
        }
    }
}
