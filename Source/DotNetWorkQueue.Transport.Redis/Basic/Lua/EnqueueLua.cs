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
using System.Globalization;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <inheritdoc />
    /// <summary>
    /// Enqueues a message
    /// </summary>
    internal class EnqueueLua: BaseLua
    {
        private const string DateTimeFormat = "MM/dd/yyyy hh:mm:ss.fff tt";
        private const string DateTimeScheduler = "MM/dd/yyyy hh:mm:ss tt";

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="EnqueueLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public EnqueueLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"local id = @field
                    
                    if @JobName ~= '' then
                        local jobExists = redis.call('hget', @JobKey, @JobName) 
                        if(jobExists) then
                            return 'JobAlreadyExists'
                        end
                        local alreadyScheduledAndRan = redis.call('hget', @JobEventKey, @JobNameScheduled)
                        if (alreadyScheduledAndRan == @ScheduledTime) then
                            return 'JobAlreadyExists'
                        end
                    end

                     if id == '' then
                        id = redis.call('INCR', @IDKey)
                     end

                     redis.call('hset', @key, id, @value) 
                     redis.call('hset', @headerskey, id, @headers) 
                     redis.call('lpush', @pendingkey, id) 
                     redis.call('hset', @metakey, id, @metavalue) 
                     redis.call('hset', @StatusKey, id, '0')
                     if @JobName ~= '' then
                        redis.call('hset', @JobKey, @JobName, id)
                        redis.call('hset', @JobIDKey, id, @JobName)

                        redis.call('hset', @JobEventKey, @JobName, @EventTime)
                        redis.call('hset', @JobEventKey, @JobNameScheduled, @ScheduledTime)
                     end
                     if @Route ~= '' then
                         redis.call('hset', @RouteIDKey, id, @Route)
                     end
                     redis.call('publish', @channel, '') 
                     return id";
        }
        /// <summary>
        /// Executes the specified message identifier.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="metaData">The meta data.</param>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="eventTime">The event time.</param>
        /// <param name="route">The route.</param>
        /// <returns></returns>
        public string Execute(string messageId, byte[] message, byte[] headers, byte[] metaData, string jobName,
             DateTimeOffset scheduledTime, DateTimeOffset eventTime, string route)
        {
            if (Connection.IsDisposed)
                return null;

            var result = TryExecute(GetParameters(messageId, message, headers, metaData, jobName, scheduledTime,
                eventTime, route));
            if (result.IsNull)
                return null;
            return (string) result;
        }
        /// <summary>
        /// Executes the specified message identifier.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="metaData">The meta data.</param>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="eventTime">The event time.</param>
        /// <param name="route">The route.</param>
        /// <returns></returns>
        public async Task<string> ExecuteAsync(string messageId, byte[] message, byte[] headers, byte[] metaData, string jobName,
             DateTimeOffset scheduledTime, DateTimeOffset eventTime, string route)
        {
            var result = await TryExecuteAsync(GetParameters(messageId, message, headers, metaData, jobName, scheduledTime,
                eventTime, route)).ConfigureAwait(false);
            if (result.IsNull)
                return null;
            return (string)result;
        }
        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="metaData">The meta data.</param>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="eventTime">The event time.</param>
        /// <param name="route">The route.</param>
        /// <returns></returns>
        private object GetParameters(string messageId, byte[] message, byte[] headers, byte[] metaData, string jobName,
            DateTimeOffset scheduledTime, DateTimeOffset eventTime, string route)
        {
            var pendingKey = !string.IsNullOrEmpty(route) ? RedisNames.PendingRoute(route) : RedisNames.Pending;
            var realRoute = string.IsNullOrEmpty(route) ? string.Empty : route;
            return new
            {
                key = (RedisKey)RedisNames.Values,
                field = messageId,
                value = (RedisValue)message,
                headers,
                headerskey = (RedisKey)RedisNames.Headers,
                pendingkey = (RedisKey)pendingKey,
                channel = RedisNames.Notification,
                metakey = (RedisKey)RedisNames.MetaData,
                metavalue = (RedisValue)metaData,
                Route = realRoute,
                IDKey = (RedisKey)RedisNames.Id,
                JobKey = (RedisKey)RedisNames.JobNames,
                JobIDKey = (RedisKey)RedisNames.JobIdNames,
                JobName = jobName,
                StatusKey = (RedisKey)RedisNames.Status,
                JobEventKey = (RedisKey)RedisNames.JobEvent,
                RouteIDKey = (RedisKey)RedisNames.Route,
                JobNameScheduled = string.Concat(jobName, "|scheduled"),
                ScheduledTime = scheduledTime.ToString(DateTimeScheduler, CultureInfo.InvariantCulture),
                EventTime = eventTime.ToString(DateTimeFormat, CultureInfo.InvariantCulture)
            };
        }
    }
}
