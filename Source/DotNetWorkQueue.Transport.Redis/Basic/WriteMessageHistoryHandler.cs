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
    public class WriteMessageHistoryHandler : IWriteMessageHistory
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;
        private readonly IBaseTransportOptions _options;

        private string HistoryHashKey(string queueId) => $"{_redisNames.Values}:history:{queueId}";
        private string HistoryIndexKey => $"{_redisNames.Values}:history:index";

        /// <inheritdoc />
        public WriteMessageHistoryHandler(IRedisConnection connection, RedisNames redisNames, IBaseTransportOptions options)
        {
            _connection = connection;
            _redisNames = redisNames;
            _options = options;
        }

        /// <summary>Returns the Redis database to use. Protected virtual to allow test seam injection.</summary>
        protected virtual IDatabase GetDb() => _connection.Connection.GetDatabase();

        /// <inheritdoc />
        public void RecordEnqueue(string queueId, string correlationId, string route, string messageType, byte[] body, byte[] headers)
        {
            if (!_options.EnableHistory) return;
            var db = GetDb();
            var now = DateTime.UtcNow;
            db.HashSet(HistoryHashKey(queueId), new[]
            {
                new HashEntry("QueueID", queueId), new HashEntry("CorrelationID", correlationId ?? ""),
                new HashEntry("Status", (int)MessageHistoryStatus.Enqueued), new HashEntry("EnqueuedUtc", now.Ticks),
                new HashEntry("StartedUtc", 0L), new HashEntry("CompletedUtc", 0L), new HashEntry("DurationMs", 0L),
                new HashEntry("ExceptionText", ""), new HashEntry("RetryCount", 0),
                new HashEntry("Route", route ?? ""), new HashEntry("MessageType", messageType ?? ""),
            });
            db.SortedSetAdd(HistoryIndexKey, queueId, now.Ticks);
        }

        /// <inheritdoc />
        public void RecordProcessingStart(string queueId)
        {
            if (!_options.EnableHistory) return;
            var db = GetDb();
            var rawStatus = db.HashGet(HistoryHashKey(queueId), "Status");
            if (!rawStatus.HasValue || (int)rawStatus != (int)MessageHistoryStatus.Enqueued) return;
            db.HashSet(HistoryHashKey(queueId), new[] { new HashEntry("Status", (int)MessageHistoryStatus.Processing), new HashEntry("StartedUtc", DateTime.UtcNow.Ticks) });
        }

        /// <inheritdoc />
        public void RecordComplete(string queueId)
        {
            if (!_options.EnableHistory) return;
            var db = GetDb();
            var now = DateTime.UtcNow;
            var startedTicks = (long)db.HashGet(HistoryHashKey(queueId), "StartedUtc");
            var durationMs = startedTicks > 0 ? (long)(now - new DateTime(startedTicks, DateTimeKind.Utc)).TotalMilliseconds : 0L;
            db.HashSet(HistoryHashKey(queueId), new[] { new HashEntry("Status", (int)MessageHistoryStatus.Complete), new HashEntry("CompletedUtc", now.Ticks), new HashEntry("DurationMs", durationMs) });
        }

        /// <inheritdoc />
        public void RecordError(string queueId, string exception)
        {
            if (!_options.EnableHistory) return;
            var db = GetDb();
            var now = DateTime.UtcNow;
            var startedTicks = (long)db.HashGet(HistoryHashKey(queueId), "StartedUtc");
            var durationMs = startedTicks > 0 ? (long)(now - new DateTime(startedTicks, DateTimeKind.Utc)).TotalMilliseconds : 0L;
            db.HashSet(HistoryHashKey(queueId), new[] { new HashEntry("Status", (int)MessageHistoryStatus.Error), new HashEntry("CompletedUtc", now.Ticks), new HashEntry("DurationMs", durationMs), new HashEntry("ExceptionText", exception ?? "") });
        }

        /// <inheritdoc />
        public void RecordRollback(string queueId)
        {
            if (!_options.EnableHistory) return;
            var db = GetDb();
            db.HashIncrement(HistoryHashKey(queueId), "RetryCount", 1);
            db.HashSet(HistoryHashKey(queueId), new[] { new HashEntry("Status", (int)MessageHistoryStatus.Enqueued), new HashEntry("StartedUtc", 0L), new HashEntry("CompletedUtc", 0L), new HashEntry("DurationMs", 0L) });
        }

        /// <inheritdoc />
        public void RecordDelete(string queueId)
        {
            if (!_options.EnableHistory) return;
            var db = GetDb();
            db.HashSet(HistoryHashKey(queueId), new[] { new HashEntry("Status", (int)MessageHistoryStatus.Deleted), new HashEntry("CompletedUtc", DateTime.UtcNow.Ticks) });
        }

        /// <inheritdoc />
        public void RecordExpire(string queueId)
        {
            if (!_options.EnableHistory) return;
            var db = GetDb();
            db.HashSet(HistoryHashKey(queueId), new[] { new HashEntry("Status", (int)MessageHistoryStatus.Expired), new HashEntry("CompletedUtc", DateTime.UtcNow.Ticks) });
        }
    }
}
