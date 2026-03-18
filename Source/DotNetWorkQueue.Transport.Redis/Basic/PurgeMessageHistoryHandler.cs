// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Purges old message history records for the Redis transport.
    /// </summary>
    public class PurgeMessageHistoryHandler : IPurgeMessageHistory
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;

        private string HistoryHashKey(string queueId) => $"{_redisNames.Values}:history:{queueId}";
        private string HistoryIndexKey => $"{_redisNames.Values}:history:index";

        /// <summary>
        /// Initializes a new instance of the <see cref="PurgeMessageHistoryHandler"/> class.
        /// </summary>
        public PurgeMessageHistoryHandler(IRedisConnection connection, RedisNames redisNames)
        {
            _connection = connection;
            _redisNames = redisNames;
        }

        /// <inheritdoc />
        public long Purge(DateTime olderThan)
        {
            var db = _connection.Connection.GetDatabase();
            var cutoffTicks = olderThan.Ticks;

            // Get all entries with score (enqueued ticks) less than cutoff
            var members = db.SortedSetRangeByScore(HistoryIndexKey, double.NegativeInfinity, cutoffTicks);

            long count = 0;
            foreach (var member in members)
            {
                var queueId = member.ToString();
                var key = HistoryHashKey(queueId);

                // Check CompletedUtc — only purge if completed before cutoff (or never completed and enqueued before cutoff)
                var completedTicks = (long)db.HashGet(key, "CompletedUtc");
                if ((completedTicks > 0 && completedTicks < cutoffTicks) || (completedTicks == 0))
                {
                    db.KeyDelete(key);
                    db.SortedSetRemove(HistoryIndexKey, queueId);
                    count++;
                }
            }

            return count;
        }
    }
}
