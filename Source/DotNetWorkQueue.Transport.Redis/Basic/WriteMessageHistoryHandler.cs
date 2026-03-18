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
    /// <summary>
    /// Writes message history records for the Redis transport.
    /// Uses a Redis hash per message and a sorted set for ordering.
    /// </summary>
    public class WriteMessageHistoryHandler : IWriteMessageHistory
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;
        private readonly IHistoryConfiguration _config;

        private string HistoryHashKey(string queueId) => $"{_redisNames.Values}:history:{queueId}";
        private string HistoryIndexKey => $"{_redisNames.Values}:history:index";

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteMessageHistoryHandler"/> class.
        /// </summary>
        public WriteMessageHistoryHandler(IRedisConnection connection,
            RedisNames redisNames,
            IHistoryConfiguration config)
        {
            _connection = connection;
            _redisNames = redisNames;
            _config = config;
        }

        /// <inheritdoc />
        public void RecordEnqueue(string queueId, string correlationId, string route, string messageType,
            byte[] body, byte[] headers)
        {
            var db = _connection.Connection.GetDatabase();
            var key = HistoryHashKey(queueId);
            var now = DateTime.UtcNow;

            db.HashSet(key, new[]
            {
                new HashEntry("QueueID", queueId),
                new HashEntry("CorrelationID", correlationId ?? ""),
                new HashEntry("Status", (int)MessageHistoryStatus.Enqueued),
                new HashEntry("EnqueuedUtc", now.Ticks),
                new HashEntry("StartedUtc", 0L),
                new HashEntry("CompletedUtc", 0L),
                new HashEntry("DurationMs", 0L),
                new HashEntry("ExceptionText", ""),
                new HashEntry("RetryCount", 0),
                new HashEntry("Route", route ?? ""),
                new HashEntry("MessageType", messageType ?? ""),
            });

            // Add to sorted set for ordering (score = enqueued ticks)
            db.SortedSetAdd(HistoryIndexKey, queueId, now.Ticks);
        }

        /// <inheritdoc />
        public void RecordProcessingStart(string queueId)
        {
            var db = _connection.Connection.GetDatabase();
            var key = HistoryHashKey(queueId);
            db.HashSet(key, new[]
            {
                new HashEntry("Status", (int)MessageHistoryStatus.Processing),
                new HashEntry("StartedUtc", DateTime.UtcNow.Ticks),
            });
        }

        /// <inheritdoc />
        public void RecordComplete(string queueId)
        {
            var db = _connection.Connection.GetDatabase();
            var key = HistoryHashKey(queueId);
            var now = DateTime.UtcNow;

            var startedTicks = (long)db.HashGet(key, "StartedUtc");
            var durationMs = startedTicks > 0 ? (long)(now - new DateTime(startedTicks, DateTimeKind.Utc)).TotalMilliseconds : 0L;

            db.HashSet(key, new[]
            {
                new HashEntry("Status", (int)MessageHistoryStatus.Complete),
                new HashEntry("CompletedUtc", now.Ticks),
                new HashEntry("DurationMs", durationMs),
            });
        }

        /// <inheritdoc />
        public void RecordError(string queueId, string exception)
        {
            var db = _connection.Connection.GetDatabase();
            var key = HistoryHashKey(queueId);
            var now = DateTime.UtcNow;

            var startedTicks = (long)db.HashGet(key, "StartedUtc");
            var durationMs = startedTicks > 0 ? (long)(now - new DateTime(startedTicks, DateTimeKind.Utc)).TotalMilliseconds : 0L;

            db.HashSet(key, new[]
            {
                new HashEntry("Status", (int)MessageHistoryStatus.Error),
                new HashEntry("CompletedUtc", now.Ticks),
                new HashEntry("DurationMs", durationMs),
                new HashEntry("ExceptionText", exception ?? ""),
            });
        }

        /// <inheritdoc />
        public void RecordRollback(string queueId)
        {
            var db = _connection.Connection.GetDatabase();
            var key = HistoryHashKey(queueId);

            db.HashIncrement(key, "RetryCount", 1);
            db.HashSet(key, new[]
            {
                new HashEntry("Status", (int)MessageHistoryStatus.Enqueued),
                new HashEntry("StartedUtc", 0L),
                new HashEntry("CompletedUtc", 0L),
                new HashEntry("DurationMs", 0L),
            });
        }

        /// <inheritdoc />
        public void RecordDelete(string queueId)
        {
            var db = _connection.Connection.GetDatabase();
            var key = HistoryHashKey(queueId);
            db.HashSet(key, new[]
            {
                new HashEntry("Status", (int)MessageHistoryStatus.Deleted),
                new HashEntry("CompletedUtc", DateTime.UtcNow.Ticks),
            });
        }

        /// <inheritdoc />
        public void RecordExpire(string queueId)
        {
            var db = _connection.Connection.GetDatabase();
            var key = HistoryHashKey(queueId);
            db.HashSet(key, new[]
            {
                new HashEntry("Status", (int)MessageHistoryStatus.Expired),
                new HashEntry("CompletedUtc", DateTime.UtcNow.Ticks),
            });
        }
    }
}
