// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
    /// Deletes a message from the queue
    /// </summary>
    internal class DeleteLua: BaseLua
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public DeleteLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script= @"redis.call('zrem', @workingkey, @uuid) 
                     redis.call('hdel', @valueskey, @uuid) 
                     redis.call('hdel', @headerskey, @uuid) 
                     redis.call('hdel', @metakey, @uuid) 
                     redis.call('LREM', @pendingkey, -1, @uuid) 
                     redis.call('LREM', @errorkey, -1, @uuid) 
                     redis.call('zrem', @delaykey, @uuid) 
                     redis.call('zrem', @expirekey, @uuid) 
                     redis.call('hdel', @StatusKey, @uuid) 

                     local jobName = redis.call('hget', @JobIDKey, @uuid) 
                     if (jobName) then
                        redis.call('hdel', @JobIDKey, @uuid) 
                        redis.call('hdel', @JobKey, jobName) 
                     end
                     return 1";
        }
        /// <summary>
        /// Deletes the specified message.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <returns></returns>
        public int? Execute(string messageId)
        {
            var db = Connection.Connection.GetDatabase();
            return (int) db.ScriptEvaluate(LoadedLuaScript, GetParameters(messageId));
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
                pendingkey = (RedisKey)RedisNames.Pending,
                errorkey = (RedisKey)RedisNames.Error,
                delaykey = (RedisKey)RedisNames.Delayed,
                expirekey = (RedisKey)RedisNames.Expiration,
                JobKey = (RedisKey)RedisNames.JobNames,
                JobIDKey = (RedisKey)RedisNames.JobIdNames,
                StatusKey = (RedisKey)RedisNames.Status,
            };
        }
    }
}
