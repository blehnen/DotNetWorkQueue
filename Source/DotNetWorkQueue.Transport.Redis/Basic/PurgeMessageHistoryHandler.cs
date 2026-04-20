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
using DotNetWorkQueue.Configuration;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <inheritdoc />
    public class PurgeMessageHistoryHandler : IPurgeMessageHistory
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;
        private readonly IBaseTransportOptions _options;

        private string HistoryHashKey(string queueId) => $"{_redisNames.Values}:history:{queueId}";
        private string HistoryIndexKey => $"{_redisNames.Values}:history:index";

        /// <inheritdoc />
        public PurgeMessageHistoryHandler(IRedisConnection connection, RedisNames redisNames, IBaseTransportOptions options)
        {
            _connection = connection;
            _redisNames = redisNames;
            _options = options;
        }

        /// <summary>Returns the Redis database to use. Protected virtual to allow test seam injection.</summary>
        protected virtual IDatabase GetDb() => _connection.Connection.GetDatabase();

        /// <inheritdoc />
        /// <remarks>
        /// Does not check <see cref="IBaseTransportOptions.EnableHistory"/>. That flag gates
        /// WRITES only; Purge iterates the current sorted set, which is naturally empty when
        /// history was never written.
        /// </remarks>
        public long Purge(DateTime olderThan)
        {
            var db = GetDb();
            var cutoffTicks = olderThan.Ticks;
            var members = db.SortedSetRangeByScore(HistoryIndexKey, double.NegativeInfinity, cutoffTicks);
            long count = 0;
            foreach (var member in members)
            {
                var queueId = member.ToString();
                var rawStatus = db.HashGet(HistoryHashKey(queueId), "Status");

                if (!rawStatus.HasValue)
                {
                    // Orphaned index entry: hash was already deleted. Clean up the index.
                    db.SortedSetRemove(HistoryIndexKey, queueId);
                    count++;
                    continue;
                }

                var rawCompleted = db.HashGet(HistoryHashKey(queueId), "CompletedUtc");
                var status = (MessageHistoryStatus)(int)rawStatus;
                var completedTicks = rawCompleted.HasValue ? (long)rawCompleted : 0L;

                var isTerminal = status == MessageHistoryStatus.Complete
                              || status == MessageHistoryStatus.Error
                              || status == MessageHistoryStatus.Deleted
                              || status == MessageHistoryStatus.Expired;

                if (isTerminal && completedTicks > 0 && completedTicks < cutoffTicks)
                {
                    db.KeyDelete(HistoryHashKey(queueId));
                    db.SortedSetRemove(HistoryIndexKey, queueId);
                    count++;
                }
            }
            return count;
        }
    }
}
