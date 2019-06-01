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

            var db = Connection.Connection.GetDatabase();
            return (string)db.ScriptEvaluate(LoadedLuaScript, GetParameters(messageId, message, headers, metaData, jobName, scheduledTime, eventTime, route));
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
            var db = Connection.Connection.GetDatabase();
            return (string) await db.ScriptEvaluateAsync(LoadedLuaScript, GetParameters(messageId, message, headers, metaData, jobName, scheduledTime, eventTime, route)).ConfigureAwait(false);
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
