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
using DotNetWorkQueue.Transport.LiteDb.Schema;

namespace DotNetWorkQueue.Transport.LiteDb.Basic
{
    /// <summary>
    /// Writes message history records for LiteDB transport.
    /// </summary>
    public class WriteMessageHistoryHandler : IWriteMessageHistory
    {
        private readonly LiteDbConnectionManager _connectionManager;
        private readonly TableNameHelper _tableNameHelper;
        private readonly IBaseTransportOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteMessageHistoryHandler"/> class.
        /// </summary>
        public WriteMessageHistoryHandler(LiteDbConnectionManager connectionManager,
            TableNameHelper tableNameHelper,
            IBaseTransportOptions options)
        {
            _connectionManager = connectionManager;
            _tableNameHelper = tableNameHelper;
            _options = options;
        }

        /// <inheritdoc />
        public void RecordEnqueue(string queueId, string correlationId, string route, string messageType,
            byte[] body, byte[] headers)
        {
            using (var db = _connectionManager.GetDatabase())
            {
                var col = db.Database.GetCollection<HistoryTable>(_tableNameHelper.HistoryName);
                col.Insert(new HistoryTable
                {
                    QueueId = queueId,
                    CorrelationId = correlationId,
                    Status = (int)MessageHistoryStatus.Enqueued,
                    EnqueuedUtc = DateTime.UtcNow.Ticks,
                    RetryCount = 0,
                    Route = route,
                    MessageType = messageType,
                    Body = _options.HistoryOptions.StoreBody ? body : null,
                    Headers = _options.HistoryOptions.StoreBody ? headers : null
                });
            }
        }

        /// <inheritdoc />
        public void RecordProcessingStart(string queueId)
        {
            using (var db = _connectionManager.GetDatabase())
            {
                var col = db.Database.GetCollection<HistoryTable>(_tableNameHelper.HistoryName);
                var record = col.FindOne(x => x.QueueId == queueId && x.Status == (int)MessageHistoryStatus.Enqueued);
                if (record != null)
                {
                    record.Status = (int)MessageHistoryStatus.Processing;
                    record.StartedUtc = DateTime.UtcNow.Ticks;
                    col.Update(record);
                }
            }
        }

        /// <inheritdoc />
        public void RecordComplete(string queueId)
        {
            if (!_options.EnableHistory) return;
            var now = DateTime.UtcNow;
            using (var db = _connectionManager.GetDatabase())
            {
                var col = db.Database.GetCollection<HistoryTable>(_tableNameHelper.HistoryName);
                var record = col.FindOne(x => x.QueueId == queueId && x.Status == (int)MessageHistoryStatus.Processing);
                if (record != null)
                {
                    record.Status = (int)MessageHistoryStatus.Complete;
                    record.CompletedUtc = now.Ticks;
                    record.DurationMs = record.StartedUtc > 0
                        ? (long)(now - new DateTime(record.StartedUtc, DateTimeKind.Utc)).TotalMilliseconds
                        : 0L;
                    col.Update(record);
                }
            }
        }

        /// <inheritdoc />
        public void RecordError(string queueId, string exception)
        {
            var now = DateTime.UtcNow;
            using (var db = _connectionManager.GetDatabase())
            {
                var col = db.Database.GetCollection<HistoryTable>(_tableNameHelper.HistoryName);
                var record = col.FindOne(x => x.QueueId == queueId &&
                    (x.Status == (int)MessageHistoryStatus.Processing || x.Status == (int)MessageHistoryStatus.Enqueued));
                if (record != null)
                {
                    record.Status = (int)MessageHistoryStatus.Error;
                    record.CompletedUtc = now.Ticks;
                    record.ExceptionText = exception;
                    record.DurationMs = record.StartedUtc > 0
                        ? (long)(now - new DateTime(record.StartedUtc, DateTimeKind.Utc)).TotalMilliseconds
                        : 0L;
                    col.Update(record);
                }
            }
        }

        /// <inheritdoc />
        public void RecordRollback(string queueId)
        {
            using (var db = _connectionManager.GetDatabase())
            {
                var col = db.Database.GetCollection<HistoryTable>(_tableNameHelper.HistoryName);
                var record = col.FindOne(x => x.QueueId == queueId);
                if (record != null)
                {
                    record.Status = (int)MessageHistoryStatus.Enqueued;
                    record.RetryCount++;
                    record.StartedUtc = 0;
                    record.CompletedUtc = 0;
                    record.DurationMs = 0;
                    col.Update(record);
                }
            }
        }

        /// <inheritdoc />
        public void RecordDelete(string queueId)
        {
            using (var db = _connectionManager.GetDatabase())
            {
                var col = db.Database.GetCollection<HistoryTable>(_tableNameHelper.HistoryName);
                var record = col.FindOne(x => x.QueueId == queueId);
                if (record != null)
                {
                    record.Status = (int)MessageHistoryStatus.Deleted;
                    record.CompletedUtc = DateTime.UtcNow.Ticks;
                    col.Update(record);
                }
            }
        }

        /// <inheritdoc />
        public void RecordExpire(string queueId)
        {
            using (var db = _connectionManager.GetDatabase())
            {
                var col = db.Database.GetCollection<HistoryTable>(_tableNameHelper.HistoryName);
                var record = col.FindOne(x => x.QueueId == queueId);
                if (record != null)
                {
                    record.Status = (int)MessageHistoryStatus.Expired;
                    record.CompletedUtc = DateTime.UtcNow.Ticks;
                    col.Update(record);
                }
            }
        }
    }
}
