using System;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <inheritdoc />
    /// <summary>
    /// Moves delayed records to the pending queue
    /// </summary>
    internal class MoveDelayedToPendingLua: BaseLua
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="MoveDelayedToPendingLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public MoveDelayedToPendingLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"local uuids = redis.call('zrangebyscore', @delaykey, 0, @currenttime, 'LIMIT', 0, @limit)
                       if #uuids == 0 then
                        return 0
                       end

                       for k, v in pairs(uuids) do                             
                        redis.call('zrem',  @delaykey, v)
                        
                        local routeName = redis.call('hget', @RouteIDKey, v) 
                        if(routeName) then
                            local routePending = @pendingkey .. '_}' .. routeName
                            redis.call('lpush', routePending, v) 
                        else
                            redis.call('lpush', @pendingkey, v) 
                        end
                       end
                      redis.call('publish', @channel, '') 
                      return table.getn(uuids)";
        }
        /// <summary>
        /// Moves delayed records to the pending queue
        /// </summary>
        /// <param name="unixTime">The unix time.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public int Execute(long unixTime, int count)
        {
            if (Connection.IsDisposed)
                return 0;

            var db = Connection.Connection.GetDatabase();
            return (int) db.ScriptEvaluate(LoadedLuaScript, GetParameters(unixTime, count));
        }
        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="unixTime">The unix time.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        private object GetParameters(long unixTime, int count)
        {
            return new
            {
                pendingkey = (RedisKey)RedisNames.Pending,
                delaykey = (RedisKey)RedisNames.Delayed,
                currenttime = unixTime,
                channel = RedisNames.Notification,
                limit = count,
                RouteIDKey = (RedisKey)RedisNames.Route
            };
        }
    }
}
