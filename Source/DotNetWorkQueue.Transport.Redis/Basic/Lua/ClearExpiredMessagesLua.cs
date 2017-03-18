// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using StackExchange.Redis;
namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <summary>
    /// Clears expired messages from the queue
    /// </summary>
    internal class ClearExpiredMessagesLua: BaseLua
    {
        public ClearExpiredMessagesLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"local uuids = redis.call('zrangebyscore', @expirekey, 0, @currenttime, 'LIMIT', 0, @limit)
                        if #uuids == 0 then
	                        return 0
                        end
                        local inProgress = 0
                        for k, v in pairs(uuids) do       
                            local inPending = redis.call('LREM', @pendingkey, -1, v)
                            if(inPending == 1) then                                             
	                            redis.call('hdel', @valueskey, v) 
                                redis.call('hdel', @headerskey, v) 
                                redis.call('hdel', @Statuskey, v) 
	                            redis.call('hdel', @metakey, v) 
	                            redis.call('LREM', @errorkey, -1, v) 
	                            redis.call('zrem', @delaykey, v) 
	                            redis.call('zrem', @expirekey, v) 
                                local jobName = redis.call('hget', @JobIDKey, v) 
                                if (jobName) then
                                   redis.call('hdel', @JobIDKey, v) 
                                   redis.call('hdel', @JobKey, jobName) 
                                end
                            else
                                local routeName = redis.call('hget', @RouteIDKey, v) 
                                if(routeName) then
                                   local routePending = @pendingkey .. '_}'.. routeName
                                   inPending = redis.call('LREM', routePending, -1, v)
                                   if(inPending == 1) then                                             
	                                   redis.call('hdel', @valueskey, v) 
                                       redis.call('hdel', @headerskey, v) 
                                       redis.call('hdel', @Statuskey, v) 
	                                   redis.call('hdel', @metakey, v) 
	                                   redis.call('LREM', @errorkey, -1, v) 
	                                   redis.call('zrem', @delaykey, v) 
	                                   redis.call('zrem', @expirekey, v) 
                                       redis.call('hdel', @RouteIDKey, v) 
                                       local jobName = redis.call('hget', @JobIDKey, v) 
                                       if (jobName) then
                                          redis.call('hdel', @JobIDKey, v) 
                                          redis.call('hdel', @JobKey, jobName) 
                                       end
                                    else
                                       inProgress = inProgress + 1
                                    end
                                else
                                    inProgress = inProgress + 1
                                end
                            end
                        end
                        return table.getn(uuids) - inProgress";
        }
        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="unixTime">The current unix time.</param>
        /// <param name="count">The maximum amount of records to process.</param>
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
        /// <param name="unixTime">The current unix time.</param>
        /// <param name="count">The maximum amount of records to process.</param>
        /// <returns></returns>
        private object GetParameters(long unixTime, int count)
        {
            return new
            {
                workingkey = (RedisKey)RedisNames.Working,
                currenttime = unixTime,
                valueskey = (RedisKey)RedisNames.Values,
                headerskey = (RedisKey)RedisNames.Headers,
                metakey = (RedisKey)RedisNames.MetaData,
                pendingkey = (RedisKey)RedisNames.Pending,
                errorkey = (RedisKey)RedisNames.Error,
                delaykey = (RedisKey)RedisNames.Delayed,
                expirekey = (RedisKey)RedisNames.Expiration,
                JobKey = (RedisKey)RedisNames.JobNames,
                JobIDKey = (RedisKey)RedisNames.JobIdNames,
                Statuskey = (RedisKey)RedisNames.Status,
                RouteIDKey = (RedisKey)RedisNames.Route,
                limit = count,
            };
        }
    }
}
