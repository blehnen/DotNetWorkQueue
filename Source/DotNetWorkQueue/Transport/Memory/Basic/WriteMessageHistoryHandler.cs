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
using System.Collections.Concurrent;
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue.Transport.Memory.Basic
{
    /// <summary>
    /// Writes message history records for the in-memory transport.
    /// </summary>
    public class WriteMessageHistoryHandler : IWriteMessageHistory
    {
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MessageHistoryRecord>> Data
            = new ConcurrentDictionary<string, ConcurrentDictionary<string, MessageHistoryRecord>>();

        private readonly IConnectionInformation _connectionInformation;
        private readonly IHistoryConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteMessageHistoryHandler"/> class.
        /// </summary>
        public WriteMessageHistoryHandler(IConnectionInformation connectionInformation,
            IHistoryConfiguration config)
        {
            _connectionInformation = connectionInformation;
            _config = config;
        }

        /// <inheritdoc />
        public void RecordEnqueue(string queueId, string correlationId, string route, string messageType,
            byte[] body, byte[] headers)
        {
            var records = GetRecords();
            records[queueId] = new MessageHistoryRecord
            {
                QueueId = queueId,
                CorrelationId = correlationId,
                Status = MessageHistoryStatus.Enqueued,
                EnqueuedUtc = DateTime.UtcNow,
                RetryCount = 0,
                Route = route,
                MessageType = messageType,
                Body = _config.StoreBody ? body : null,
                Headers = _config.StoreBody ? headers : null
            };
        }

        /// <inheritdoc />
        public void RecordProcessingStart(string queueId)
        {
            var records = GetRecords();
            if (records.TryGetValue(queueId, out var record))
            {
                record.Status = MessageHistoryStatus.Processing;
                record.StartedUtc = DateTime.UtcNow;
            }
        }

        /// <inheritdoc />
        public void RecordComplete(string queueId)
        {
            var records = GetRecords();
            if (records.TryGetValue(queueId, out var record))
            {
                var now = DateTime.UtcNow;
                record.Status = MessageHistoryStatus.Complete;
                record.CompletedUtc = now;
                if (record.StartedUtc.HasValue)
                    record.DurationMs = (long)(now - record.StartedUtc.Value).TotalMilliseconds;
            }
        }

        /// <inheritdoc />
        public void RecordError(string queueId, string exception)
        {
            var records = GetRecords();
            if (records.TryGetValue(queueId, out var record))
            {
                var now = DateTime.UtcNow;
                record.Status = MessageHistoryStatus.Error;
                record.CompletedUtc = now;
                record.ExceptionText = exception;
                if (record.StartedUtc.HasValue)
                    record.DurationMs = (long)(now - record.StartedUtc.Value).TotalMilliseconds;
            }
        }

        /// <inheritdoc />
        public void RecordRollback(string queueId)
        {
            var records = GetRecords();
            if (records.TryGetValue(queueId, out var record))
            {
                record.Status = MessageHistoryStatus.Enqueued;
                record.RetryCount++;
                record.StartedUtc = null;
                record.CompletedUtc = null;
                record.DurationMs = null;
            }
        }

        /// <inheritdoc />
        public void RecordDelete(string queueId)
        {
            var records = GetRecords();
            if (records.TryGetValue(queueId, out var record))
            {
                record.Status = MessageHistoryStatus.Deleted;
                record.CompletedUtc = DateTime.UtcNow;
            }
        }

        /// <inheritdoc />
        public void RecordExpire(string queueId)
        {
            var records = GetRecords();
            if (records.TryGetValue(queueId, out var record))
            {
                record.Status = MessageHistoryStatus.Expired;
                record.CompletedUtc = DateTime.UtcNow;
            }
        }

        internal static ConcurrentDictionary<string, MessageHistoryRecord> GetRecordsForQueue(string key)
        {
            return Data.TryGetValue(key, out var records) ? records : null;
        }

        private ConcurrentDictionary<string, MessageHistoryRecord> GetRecords()
        {
            var key = $"{_connectionInformation.QueueName}|{_connectionInformation.ConnectionString}";
            return Data.GetOrAdd(key, _ => new ConcurrentDictionary<string, MessageHistoryRecord>());
        }
    }
}
