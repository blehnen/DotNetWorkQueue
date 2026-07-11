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
using System.Collections.Generic;
using System.Linq;
using DotNetWorkQueue.Configuration;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <inheritdoc />
    public class QueryMessageHistoryHandler : IQueryMessageHistory
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;

        private string HistoryHashKey(string queueId) => $"{_redisNames.Values}:history:{queueId}";
        private string HistoryIndexKey => $"{_redisNames.Values}:history:index";

        /// <inheritdoc />
        public QueryMessageHistoryHandler(IRedisConnection connection, RedisNames redisNames)
        {
            _connection = connection;
            _redisNames = redisNames;
        }

        /// <summary>Returns the Redis database to use. Protected virtual to allow test seam injection.</summary>
        protected virtual IDatabase GetDb() => _connection.Connection.GetDatabase();

        /// <inheritdoc />
        /// <remarks>
        /// Does not check <see cref="IBaseTransportOptions.EnableHistory"/>. That flag gates
        /// WRITES only; reads return whatever is in Redis. Missing sorted set → empty result.
        /// </remarks>
        public IReadOnlyList<MessageHistoryRecord> Get(int pageIndex, int pageSize, MessageHistoryStatus? statusFilter)
        {
            var db = GetDb();

            // Unfiltered: use Redis server-side pagination — exact range, single call
            if (!statusFilter.HasValue)
            {
                var start = pageIndex * pageSize;
                var stop = start + pageSize - 1;
                var members = db.SortedSetRangeByRank(HistoryIndexKey, start, stop, Order.Descending);
                var results = new List<MessageHistoryRecord>(members.Length);
                foreach (var member in members)
                {
                    var record = LoadRecord(db, member.ToString());
                    if (record != null) results.Add(record);
                }
                return results;
            }

            // Filtered: scan in batches since status is in the hash, not the sorted set score
            const int batchSize = 100;
            var filteredResults = new List<MessageHistoryRecord>();
            var skip = pageIndex * pageSize;
            var skipped = 0;
            long batchStart = 0;
            while (true)
            {
                var members = db.SortedSetRangeByRank(HistoryIndexKey, batchStart, batchStart + batchSize - 1, Order.Descending);
                if (members.Length == 0) break;
                foreach (var member in members)
                {
                    var record = LoadRecord(db, member.ToString());
                    if (record == null || record.Status != statusFilter.Value) continue;
                    if (skipped < skip) { skipped++; continue; }
                    filteredResults.Add(record);
                    if (filteredResults.Count >= pageSize) return filteredResults;
                }
                batchStart += batchSize;
            }
            return filteredResults;
        }

        /// <inheritdoc />
        public MessageHistoryRecord GetByQueueId(string queueId)
        {
            var db = GetDb();
            return LoadRecord(db, queueId);
        }

        /// <inheritdoc />
        public long GetCount(MessageHistoryStatus? statusFilter)
        {
            var db = GetDb();
            if (!statusFilter.HasValue) return db.SortedSetLength(HistoryIndexKey);
            var members = db.SortedSetRangeByRank(HistoryIndexKey, 0, -1);
            return members.Count(m => { var s = (int)db.HashGet(HistoryHashKey(m.ToString()), "Status"); return s == (int)statusFilter.Value; });
        }

        private MessageHistoryRecord LoadRecord(IDatabase db, string queueId)
        {
            var entries = db.HashGetAll(HistoryHashKey(queueId));
            if (entries.Length == 0) return null;
            var dict = entries.ToDictionary(e => e.Name.ToString(), e => e.Value);
            var startedTicks = GetLong(dict, "StartedUtc");
            var completedTicks = GetLong(dict, "CompletedUtc");
            var durationMs = GetLong(dict, "DurationMs");
            return new MessageHistoryRecord
            {
                QueueId = GetString(dict, "QueueID"), CorrelationId = NullIfEmpty(GetString(dict, "CorrelationID")),
                Status = (MessageHistoryStatus)(int)GetLong(dict, "Status"),
                EnqueuedUtc = new DateTime(GetLong(dict, "EnqueuedUtc"), DateTimeKind.Utc),
                StartedUtc = startedTicks > 0 ? new DateTime(startedTicks, DateTimeKind.Utc) : (DateTime?)null,
                CompletedUtc = completedTicks > 0 ? new DateTime(completedTicks, DateTimeKind.Utc) : (DateTime?)null,
                DurationMs = completedTicks > 0 ? durationMs : (long?)null,
                ExceptionText = NullIfEmpty(GetString(dict, "ExceptionText")),
                RetryCount = (int)GetLong(dict, "RetryCount"),
                Route = NullIfEmpty(GetString(dict, "Route")), MessageType = NullIfEmpty(GetString(dict, "MessageType"))
            };
        }

        private static string GetString(Dictionary<string, RedisValue> dict, string key)
        { return dict.TryGetValue(key, out var val) ? val.ToString() : ""; }
        private static long GetLong(Dictionary<string, RedisValue> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val.TryParse(out long result))
                return result;
            return 0L;
        }
        private static string NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;
    }
}
