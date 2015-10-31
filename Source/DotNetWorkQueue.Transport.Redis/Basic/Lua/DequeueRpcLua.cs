// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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

using System.Threading.Tasks;
using StackExchange.Redis;
namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <summary>
    /// Dequeues the next record for a Rpc
    /// </summary>
    internal class DequeueRpcLua: BaseLua
    {
        public DequeueRpcLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script= @"local count = redis.call('LREM', @pendingkey, 1, @uuid) 
                    if (count==0) then 
                        return nil;
                    end                   
                    local expireScore = redis.call('zscore', @expirekey, @uuid)
                    redis.call('zadd', @workingkey, @timestamp, @uuid) 
                    local message = redis.call('hget', @valueskey, @uuid) 
                    local headers = redis.call('hget', @headerskey, @uuid)
                    return {@uuid, message, headers, expireScore}";
        }
        /// <summary>
        /// Dequeues the next record for a Rpc.
        /// </summary>
        /// <param name="messageid">The messageid.</param>
        /// <param name="unixTime">The current unix time.</param>
        /// <returns></returns>
        public RedisValue[] Execute(string messageid, long unixTime)
        {
            if (Connection.IsDisposed)
                return null;

            var db = Connection.Connection.GetDatabase();
            return (RedisValue[])db.ScriptEvaluate(LoadedLuaScript, GetParameters(messageid, unixTime));
        }
        /// <summary>
        /// Dequeues the next record for a Rpc.
        /// </summary>
        /// <param name="messageid">The messageid.</param>
        /// <param name="unixTime">The current unix time.</param>
        /// <returns></returns>
        public async Task<RedisValue[]> ExecuteAsync(string messageid, long unixTime)
        {
            if (Connection.IsDisposed)
                return null;

            var db = Connection.Connection.GetDatabase();
            var result = await db.ScriptEvaluateAsync(LoadedLuaScript, GetParameters(messageid, unixTime)).ConfigureAwait(false);
            return (RedisValue[])result;
        }
        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="messageid">The messageid.</param>
        /// <param name="unixTime">The current unix time.</param>
        /// <returns></returns>
        private object GetParameters(string messageid, long unixTime)
        {
            return new
            {
                pendingkey = (RedisKey)RedisNames.Pending,
                workingkey = (RedisKey)RedisNames.Working,
                timestamp = unixTime,
                headerskey = (RedisKey)RedisNames.Headers,
                valueskey = (RedisKey)RedisNames.Values,
                expirekey = (RedisKey)RedisNames.Expiration,
                uuid = messageid
            };
        }
    }
}
