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
        private readonly IBaseTransportOptions _options;

        public WriteMessageHistoryHandler(IConnectionInformation connectionInformation, IBaseTransportOptions options)
        {
            _connectionInformation = connectionInformation;
            _options = options;
        }

        /// <inheritdoc />
        public void RecordEnqueue(string queueId, string correlationId, string route, string messageType, byte[] body, byte[] headers)
        {
            if (!_options.EnableHistory) return;
            var records = GetRecords();
            records[queueId] = new MessageHistoryRecord
            {
                QueueId = queueId, CorrelationId = correlationId, Status = MessageHistoryStatus.Enqueued,
                EnqueuedUtc = DateTime.UtcNow, RetryCount = 0, Route = route, MessageType = messageType,
                Body = _options.HistoryOptions.StoreBody ? body : null, Headers = _options.HistoryOptions.StoreBody ? headers : null
            };
        }

        /// <inheritdoc />
        public void RecordProcessingStart(string queueId)
        {
            if (!_options.EnableHistory) return;
            if (GetRecords().TryGetValue(queueId, out var r)) { r.Status = MessageHistoryStatus.Processing; r.StartedUtc = DateTime.UtcNow; }
        }

        /// <inheritdoc />
        public void RecordComplete(string queueId)
        {
            if (!_options.EnableHistory) return;
            if (GetRecords().TryGetValue(queueId, out var r))
            {
                var now = DateTime.UtcNow;
                r.Status = MessageHistoryStatus.Complete; r.CompletedUtc = now;
                if (r.StartedUtc.HasValue) r.DurationMs = (long)(now - r.StartedUtc.Value).TotalMilliseconds;
            }
        }

        /// <inheritdoc />
        public void RecordError(string queueId, string exception)
        {
            if (!_options.EnableHistory) return;
            if (GetRecords().TryGetValue(queueId, out var r))
            {
                var now = DateTime.UtcNow;
                r.Status = MessageHistoryStatus.Error; r.CompletedUtc = now; r.ExceptionText = exception;
                if (r.StartedUtc.HasValue) r.DurationMs = (long)(now - r.StartedUtc.Value).TotalMilliseconds;
            }
        }

        /// <inheritdoc />
        public void RecordRollback(string queueId)
        {
            if (!_options.EnableHistory) return;
            if (GetRecords().TryGetValue(queueId, out var r))
            { r.Status = MessageHistoryStatus.Enqueued; r.RetryCount++; r.StartedUtc = null; r.CompletedUtc = null; r.DurationMs = null; }
        }

        /// <inheritdoc />
        public void RecordDelete(string queueId)
        {
            if (!_options.EnableHistory) return;
            if (GetRecords().TryGetValue(queueId, out var r)) { r.Status = MessageHistoryStatus.Deleted; r.CompletedUtc = DateTime.UtcNow; }
        }

        /// <inheritdoc />
        public void RecordExpire(string queueId)
        {
            if (!_options.EnableHistory) return;
            if (GetRecords().TryGetValue(queueId, out var r)) { r.Status = MessageHistoryStatus.Expired; r.CompletedUtc = DateTime.UtcNow; }
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
