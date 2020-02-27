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
