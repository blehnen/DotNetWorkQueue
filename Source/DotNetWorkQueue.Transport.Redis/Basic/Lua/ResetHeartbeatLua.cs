using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <inheritdoc />
    /// <summary>
    /// Resets the heartbeat for records outside of the window.
    /// </summary>
    internal class ResetHeartbeatLua : BaseLua
    {
        private readonly IGetTime _getTime;
        private readonly ICompositeSerialization _serialization;

        /// <inheritdoc />
        public ResetHeartbeatLua(IRedisConnection connection, RedisNames redisNames, IGetTimeFactory getTime, ICompositeSerialization serialization)
            : base(connection, redisNames)
        {
            _getTime = getTime.Create();
            _serialization = serialization;
            Script = @"local signal = tonumber(@signalID)
                        local returnData = {}
                        local uuids = redis.call('zrangebyscore', @workingkey, 0, @heartbeattime, 'LIMIT', 0, @limit)
                        if #uuids == 0 then
	                        return nil
                        end
                        local index = 1
                        for k, v in pairs(uuids) do                             
	                        redis.call('zrem',  @workingkey, v)
                            local routeName = redis.call('hget', @RouteIDKey, v) 
                            if(routeName) then
                                local routePending = @pendingkey .. '_}' .. routeName
                                redis.call('rpush', routePending, v) 
                            else
	                            redis.call('rpush', @pendingkey, v) 
                            end
                            redis.call('hset', @StatusKey, v, '0') 
	                        if signal == 1 then
		                        redis.call('publish', @channel, v) 
	                        end
                            returnData[index] = {v, redis.call('hget', @headerskey, v)}
                            index = index + 1
                        end

                        if signal == 0 then
	                        redis.call('publish', @channel, '') 
                        end
                        return returnData";
        }
        /// <summary>
        /// Resets the heartbeat for records outside of the window.
        /// </summary>
        /// <param name="unixTime">The unix time.</param>
        /// <param name="count">The count.</param>
        /// <param name="rpc">if set to <c>true</c> [RPC].</param>
        /// <returns></returns>
        public List<ResetHeartBeatOutput> Execute(long unixTime, int count, bool rpc)
        {
            if (Connection.IsDisposed)
                return new List<ResetHeartBeatOutput>();

            var db = Connection.Connection.GetDatabase();
            DateTime Start = _getTime.GetCurrentUtcDate();
            var result = db.ScriptEvaluate(LoadedLuaScript, GetParameters(unixTime, count, rpc));
            DateTime End = _getTime.GetCurrentUtcDate();
            if(result.IsNull) return new List<ResetHeartBeatOutput>(0);
            var ids = (RedisResult[]) result;
            var returnData = new List<ResetHeartBeatOutput>(ids.Length);
            foreach (RedisResult[] id in ids)
            {
                var queueId = (string)id[0];
                var header = (byte[])id[1];
                IDictionary<string, object> headers = null;
                if(header != null)
                    headers = _serialization.InternalSerializer.ConvertBytesTo<IDictionary<string, object>>(header);

                if(headers != null)
                    returnData.Add(new ResetHeartBeatOutput(new RedisQueueId(queueId), new ReadOnlyDictionary<string, object>(headers), Start, End));
                returnData.Add(new ResetHeartBeatOutput(new RedisQueueId(queueId), null, Start, End));
            }

            return returnData;
        }
        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="heartbeatResetTime">The heartbeat reset time.</param>
        /// <param name="count">The count.</param>
        /// <param name="rpc">if set to <c>true</c> [RPC].</param>
        /// <returns></returns>
        private object GetParameters(long heartbeatResetTime, int count, bool rpc)
        {
            return new
            {
                pendingkey = (RedisKey)RedisNames.Pending,
                workingkey = (RedisKey)RedisNames.Working,
                heartbeattime = heartbeatResetTime,
                channel = RedisNames.Notification,
                limit = count,
                signalID = Convert.ToInt32(rpc),
                RouteIDKey = (RedisKey)RedisNames.Route,
                StatusKey = (RedisKey)RedisNames.Status,
                headerskey = (RedisKey)RedisNames.Headers,
            };
        }
    }
}
