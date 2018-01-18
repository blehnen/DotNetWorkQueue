using System;
using System.Globalization;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <inheritdoc />
    /// <seealso cref="DotNetWorkQueue.IJobSchedulerLastKnownEvent" />
    public class RedisJobSchedulerLastKnownEvent : IJobSchedulerLastKnownEvent
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;
        private const string DateTimeFormat = "MM/dd/yyyy hh:mm:ss.fff tt";

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisJobSchedulerLastKnownEvent" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public RedisJobSchedulerLastKnownEvent(
            IRedisConnection connection,
            RedisNames redisNames)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);

            _connection = connection;
            _redisNames = redisNames;
        }

        /// <inheritdoc />
        public DateTimeOffset Get(string jobName)
        {
            var db = _connection.Connection.GetDatabase();
            var time = (string)db.HashGet(_redisNames.JobEvent, jobName);
            if(!string.IsNullOrWhiteSpace(time))
            {
                try
                {
                    return DateTimeOffset.ParseExact(time, DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                }
                catch (Exception e)
                {
                    throw new DotNetWorkQueueException($"input {time} is not a valid date/time in {DateTimeFormat} format", e);
                }
            }
            return default(DateTimeOffset);
        }
    }
}
