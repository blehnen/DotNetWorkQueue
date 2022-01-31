// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
            if (!string.IsNullOrWhiteSpace(time))
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
