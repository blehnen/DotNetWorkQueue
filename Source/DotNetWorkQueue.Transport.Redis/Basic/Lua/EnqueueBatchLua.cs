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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MsgPack;
using MsgPack.Serialization;
using StackExchange.Redis;
using System.Linq;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <summary>
    /// Enqueues multiple records at once
    /// </summary>
    public class EnqueueBatchLua: BaseLua
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnqueueBatchLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public EnqueueBatchLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"local messages = cmsgpack.unpack(@messages)
                        local a={}
                        local i = 1
                        local signal = 0

                        for k1, v1 in pairs(messages) do    
	                        local id
	                        local correlationId
	                        local message
                            local headers
	                        local metadata
	                        local timestamp
	                        local expiretime
                            local route

	                        for k2, v2 in pairs(v1) do     
		                        local key = tonumber(k2)   
		                        if key == 1 then
			                        correlationId = v2
		                        end 
		                        if key == 2 then
			                        expiretime = tonumber(v2)
		                        end
                                if key == 3 then
			                        headers = v2
		                        end
		                        if key == 4 then
			                        message = v2
		                        end
		                        if key == 5 then
			                        id = v2
		                        end
		                        if key == 6 then
			                        metadata = v2
		                        end
                                if key == 7 then
			                        route = v2
		                        end
		                        if key == 8 then
			                        timestamp = tonumber(v2)
		                        end
	                        end

	                        if id == '' then
		                        id = redis.call('INCR', @IDKey)
	                        end

	                        a[i] = tostring(id)   
	                        a[i+1] = correlationId                         

	                        redis.call('hset', @key, id, message) 
	                        redis.call('hset', @metakey, id, metadata) 
                            redis.call('hset', @headerskey, id, headers) 
                            redis.call('hset', @StatusKey, id, '0') 

	                        if timestamp > 0 then
		                        redis.call('zadd', @delaykey, timestamp, id) 
	                        else
		                        redis.call('lpush', @pendingkey, id) 
		                        signal = 1
	                        end

                            if route ~= '' then
                               redis.call('hset', @RouteIDKey, id, route)
                            end

	                        if expiretime > 0 then
		                        redis.call('zadd', @expirekey, expiretime, id) 
	                        end
	                        i = i + 2
                        end

                        if signal == 1 then
	                        redis.call('publish', @channel, '') 
                        end
                        return cmsgpack.pack(a)";
        }
        /// <summary>
        /// Enqueues multiple records at once
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        public List<string> Execute(List<MessageToSend> messages)
        {
            if (Connection.IsDisposed)
                return null;

            var db = Connection.Connection.GetDatabase();

            //we need to group by route
            var splitList = messages.GroupBy(x => x.Route);
            var returnData = new List<string>(messages.Count);
            foreach (var group in splitList)
            {
                var result = (byte[])db.ScriptEvaluate(LoadedLuaScript, GetParameters(group.ToList(), group.Key));
                if (result == null || result.Length <= 0) continue;
                var serializer = SerializationContext.Default.GetSerializer<List<string>>();
                var tempData = serializer.UnpackSingleObject(result);
                returnData.AddRange(tempData);
            }
            return returnData;
        }

        /// <summary>
        /// Enqueues multiple records at once
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        public async Task<List<string>> ExecuteAsync(List<MessageToSend> messages)
        {
            if (Connection.IsDisposed)
                return null;

            var db = Connection.Connection.GetDatabase();

            //we need to group by route
            var splitList = messages.GroupBy(x => x.Route);
            var returnData = new List<string>(messages.Count);
            foreach (var group in splitList)
            {
                var result = (byte[])await db.ScriptEvaluateAsync(LoadedLuaScript, GetParameters(group.ToList(), group.Key)).ConfigureAwait(false);
                if (result == null || result.Length <= 0) continue;
                var serializer = SerializationContext.Default.GetSerializer<List<string>>();
                var tempData = serializer.UnpackSingleObject(result);
                returnData.AddRange(tempData);
            }
            return returnData;
        }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <param name="route">The route.</param>
        /// <returns></returns>
        private object GetParameters(List<MessageToSend> messages, string route)
        {
            var pendingKey = !string.IsNullOrEmpty(route) ? RedisNames.PendingRoute(route) : RedisNames.Pending;
            // Creates serializer.
            var serializer = SerializationContext.Default.GetSerializer<List<MessageToSend>>();
            using (var output = new MemoryStream())
            {
                serializer.Pack(output, messages, PackerCompatibilityOptions.Classic);
                object rc = new
                {
                    messages = (RedisValue) output.ToArray(),
                    key = (RedisKey) RedisNames.Values,
                    pendingkey = (RedisKey)pendingKey,
                    headerskey = (RedisKey)RedisNames.Headers,
                    channel = RedisNames.Notification,
                    metakey = (RedisKey) RedisNames.MetaData,
                    IDKey = (RedisKey) RedisNames.Id,
                    delaykey = (RedisKey)RedisNames.Delayed,
                    expirekey = (RedisKey)RedisNames.Expiration,
                    StatusKey = (RedisKey)RedisNames.Status,
                    RouteIDKey = (RedisKey)RedisNames.Route,
                };
                return rc;
            }
        }

        /// <summary>
        /// The message and any associated meta data to be passed to the LUA script.
        /// </summary>
        /// <remarks>
        /// This class must be public for MsgPack to function
        /// </remarks>
        public class MessageToSend
        {
            private string _route;
            /// <summary>
            /// Initializes a new instance of the <see cref="MessageToSend"/> class.
            /// </summary>
            public MessageToSend()
            {
                ExpireTimeStamp = 0;
                TimeStamp = 0;
            }
            /// <summary>
            /// Gets or sets the correlation identifier.
            /// </summary>
            /// <value>
            /// The correlation identifier.
            /// </value>
            public string CorrelationId { get; set; }
            /// <summary>
            /// Gets or sets the expire time stamp.
            /// </summary>
            /// <value>
            /// The expire time stamp.
            /// </value>
            /// <remarks>0 is no expiration</remarks>
            public long ExpireTimeStamp { get; set; }

            /// <summary>
            /// Gets or sets the message.
            /// </summary>
            /// <value>
            /// The message.
            /// </value>
            public MessagePackObject Message { get; set; }

            /// <summary>
            /// Gets or sets the message identifier.
            /// </summary>
            /// <value>
            /// The message identifier.
            /// </value>
            public string MessageId { get; set; }
            /// <summary>
            /// Gets or sets the meta data.
            /// </summary>
            /// <value>
            /// The meta data.
            /// </value>
            public MessagePackObject MetaData { get; set; }
            /// <summary>
            /// Gets or sets the time stamp.
            /// </summary>
            /// <value>
            /// The time stamp.
            /// </value>
            /// <remarks>0 is no delay</remarks>
            public long TimeStamp { get; set; }

            /// <summary>
            /// Gets or sets the headers.
            /// </summary>
            /// <value>
            /// The headers.
            /// </value>
            public MessagePackObject Headers { get; set; }

            /// <summary>
            /// Gets or sets the route.
            /// </summary>
            /// <value>
            /// The route.
            /// </value>
            /// <remarks>Optional</remarks>
            public string Route
            {
                get { return _route; }
                set
                {
                    _route = value ?? string.Empty;
                }
            }
        }
    }
}
