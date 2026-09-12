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
using System.Threading.Tasks;
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
        //Time comes from the configured provider rather than the local clock. On SQL Server and
        //PostgreSQL that provider is the database server, which is where the rest of the queue's
        //timestamps come from. History written from an application machine with a drifting clock
        //would otherwise disagree with the data beside it, and a duration taken from a start and an
        //end on different clocks can come out negative. BaseTime caches an offset, so this costs no
        //round trip.
        private readonly IGetTime _getTime;

        /// <inheritdoc />
        public WriteMessageHistoryHandler(IConnectionInformation connectionInformation, IBaseTransportOptions options,
            IGetTimeFactory getTimeFactory)
        {
            _connectionInformation = connectionInformation;
            _options = options;
            _getTime = getTimeFactory.Create();
        }

        /// <inheritdoc />
        public void RecordEnqueue(string queueId, string correlationId, string route, string messageType, byte[] body, byte[] headers)
        {
            if (!_options.EnableHistory) return;
            var records = GetRecords();
            records[queueId] = new MessageHistoryRecord
            {
                QueueId = queueId, CorrelationId = correlationId, Status = MessageHistoryStatus.Enqueued,
                EnqueuedUtc = _getTime.GetCurrentUtcDate(), RetryCount = 0, Route = route, MessageType = messageType,
                Body = _options.HistoryOptions.StoreBody ? body : null, Headers = _options.HistoryOptions.StoreBody ? headers : null
            };
        }

        /// <inheritdoc />
        public void RecordProcessingStart(string queueId)
        {
            if (!_options.EnableHistory) return;
            if (GetRecords().TryGetValue(queueId, out var r) && r.Status == MessageHistoryStatus.Enqueued) { r.Status = MessageHistoryStatus.Processing; r.StartedUtc = _getTime.GetCurrentUtcDate(); }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Correct as written rather than unfinished: the history lives in process, so there is no
        /// I/O to release the thread for.
        /// </remarks>
        public Task RecordProcessingStartAsync(string queueId)
        {
            RecordProcessingStart(queueId);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        /// <remarks>See <see cref="RecordProcessingStartAsync"/>: in process, nothing to release.</remarks>
        public Task RecordCompleteAsync(string queueId)
        {
            RecordComplete(queueId);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        /// <remarks>See <see cref="RecordProcessingStartAsync"/>: in process, nothing to release.</remarks>
        public Task RecordRollbackAsync(string queueId)
        {
            RecordRollback(queueId);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        /// <remarks>See <see cref="RecordProcessingStartAsync"/>: in process, nothing to release.</remarks>
        public Task RecordErrorAsync(string queueId, string exception)
        {
            RecordError(queueId, exception);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void RecordComplete(string queueId)
        {
            if (!_options.EnableHistory) return;
            if (GetRecords().TryGetValue(queueId, out var r))
            {
                var now = _getTime.GetCurrentUtcDate();
                r.Status = MessageHistoryStatus.Complete; r.CompletedUtc = now;
                if (r.StartedUtc.HasValue) r.DurationMs = (long)(now - r.StartedUtc.Value).TotalMilliseconds;
                else r.DurationMs = 0;
            }
        }

        /// <inheritdoc />
        public void RecordError(string queueId, string exception)
        {
            if (!_options.EnableHistory) return;
            if (GetRecords().TryGetValue(queueId, out var r))
            {
                var now = _getTime.GetCurrentUtcDate();
                r.Status = MessageHistoryStatus.Error; r.CompletedUtc = now; r.ExceptionText = exception;
                if (r.StartedUtc.HasValue) r.DurationMs = (long)(now - r.StartedUtc.Value).TotalMilliseconds;
                else r.DurationMs = 0;
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
            if (GetRecords().TryGetValue(queueId, out var r)) { r.Status = MessageHistoryStatus.Deleted; r.CompletedUtc = _getTime.GetCurrentUtcDate(); }
        }

        /// <inheritdoc />
        public void RecordExpire(string queueId)
        {
            if (!_options.EnableHistory) return;
            if (GetRecords().TryGetValue(queueId, out var r)) { r.Status = MessageHistoryStatus.Expired; r.CompletedUtc = _getTime.GetCurrentUtcDate(); }
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
