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
using System;
using System.Globalization;
using DotNetWorkQueue.Exceptions;
namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Gets and sets the last event time for scheduled jobs
    /// </summary>
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

        /// <summary>
        /// Gets the last known event time for the specified job.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <returns></returns>
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
