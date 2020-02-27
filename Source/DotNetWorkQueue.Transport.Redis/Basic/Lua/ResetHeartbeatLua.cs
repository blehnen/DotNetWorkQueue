// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
            Script = @"local returnData = {}
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
                            returnData[index] = {v, redis.call('hget', @headerskey, v)}
                            index = index + 1
                        end
                        redis.call('publish', @channel, '') 
                        return returnData";
        }
        /// <summary>
        /// Resets the heartbeat for records outside of the window.
        /// </summary>
        /// <param name="unixTime">The unix time.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public List<ResetHeartBeatOutput> Execute(long unixTime, int count)
        {
            if (Connection.IsDisposed)
                return new List<ResetHeartBeatOutput>();

            var db = Connection.Connection.GetDatabase();
            DateTime start = _getTime.GetCurrentUtcDate();
            var result = db.ScriptEvaluate(LoadedLuaScript, GetParameters(unixTime, count));
            DateTime end = _getTime.GetCurrentUtcDate();
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
                    returnData.Add(new ResetHeartBeatOutput(new RedisQueueId(queueId), new ReadOnlyDictionary<string, object>(headers), start, end));
                returnData.Add(new ResetHeartBeatOutput(new RedisQueueId(queueId), null, start, end));
            }

            return returnData;
        }
        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="heartbeatResetTime">The heartbeat reset time.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        private object GetParameters(long heartbeatResetTime, int count)
        {
            return new
            {
                pendingkey = (RedisKey)RedisNames.Pending,
                workingkey = (RedisKey)RedisNames.Working,
                heartbeattime = heartbeatResetTime,
                channel = RedisNames.Notification,
                limit = count,
                RouteIDKey = (RedisKey)RedisNames.Route,
                StatusKey = (RedisKey)RedisNames.Status,
                headerskey = (RedisKey)RedisNames.Headers,
            };
        }
    }
}
