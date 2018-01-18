using System;
using System.Globalization;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <inheritdoc />
    public class DoesJobExistLua: BaseLua
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="DoesJobExistLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public DoesJobExistLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"local jobExists = redis.call('hget', @JobJey, @JobName) 
                     if(jobExists) then
                        local alreadyScheduledAndRan = redis.call('hget', @JobEventKey, @JobNameScheduled)
                        if (alreadyScheduledAndRan == @ScheduledTime) then
                            return 3
                        else
                            return redis.call('hget', @StatusKey, jobExists)
                        end
                     end
                     local alreadyScheduledAndRan = redis.call('hget', @JobEventKey, @JobNameScheduled)
                     if (alreadyScheduledAndRan == @ScheduledTime) then
                         return 3
                     end
                     return -1";
        }
        /// <summary>
        /// Returns the status of the job
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <returns></returns>
        public QueueStatuses Execute(string jobName, DateTimeOffset scheduledTime)
        {
            if (Connection.IsDisposed)
                return QueueStatuses.NotQueued;

            var db = Connection.Connection.GetDatabase();
            return (QueueStatuses)(int)db.ScriptEvaluate(LoadedLuaScript, GetParameters(jobName, scheduledTime));
        }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <returns></returns>
        private object GetParameters(string jobName,
            DateTimeOffset scheduledTime)
        {
            return new
            {
                JobJey = (RedisKey) RedisNames.JobNames,
                JobName = jobName,
                StatusKey = (RedisKey) RedisNames.Status,
                JobEventKey = (RedisKey) RedisNames.JobEvent,
                JobNameScheduled = string.Concat(jobName, "|scheduled"),
                ScheduledTime = scheduledTime.ToString(CultureInfo.InvariantCulture)
            };
        }
    }
}
