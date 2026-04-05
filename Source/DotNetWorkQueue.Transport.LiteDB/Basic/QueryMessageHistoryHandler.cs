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
using DotNetWorkQueue.Transport.LiteDb.Schema;

namespace DotNetWorkQueue.Transport.LiteDb.Basic
{
    /// <summary>
    /// Queries message history records for LiteDB transport.
    /// </summary>
    public class QueryMessageHistoryHandler : IQueryMessageHistory
    {
        private readonly LiteDbConnectionManager _connectionManager;
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryMessageHistoryHandler"/> class.
        /// </summary>
        public QueryMessageHistoryHandler(LiteDbConnectionManager connectionManager,
            TableNameHelper tableNameHelper)
        {
            _connectionManager = connectionManager;
            _tableNameHelper = tableNameHelper;
        }

        /// <inheritdoc />
        public IReadOnlyList<MessageHistoryRecord> Get(int pageIndex, int pageSize, MessageHistoryStatus? statusFilter)
        {
            using (var db = _connectionManager.GetDatabase())
            {
                var col = db.Database.GetCollection<HistoryTable>(_tableNameHelper.HistoryName);
                var query = statusFilter.HasValue
                    ? col.Find(x => x.Status == (int)statusFilter.Value)
                    : col.FindAll();

                return query
                    .OrderByDescending(x => x.EnqueuedUtc)
                    .Skip(pageIndex * pageSize)
                    .Take(pageSize)
                    .Select(MapRecord)
                    .ToList();
            }
        }

        /// <inheritdoc />
        public MessageHistoryRecord GetByQueueId(string queueId)
        {
            using (var db = _connectionManager.GetDatabase())
            {
                var col = db.Database.GetCollection<HistoryTable>(_tableNameHelper.HistoryName);
                var record = col.FindOne(x => x.QueueId == queueId);
                return record != null ? MapRecord(record) : null;
            }
        }

        /// <inheritdoc />
        public long GetCount(MessageHistoryStatus? statusFilter)
        {
            using (var db = _connectionManager.GetDatabase())
            {
                var col = db.Database.GetCollection<HistoryTable>(_tableNameHelper.HistoryName);
                if (!statusFilter.HasValue)
                    return col.Count();
                // Use FindAll + LINQ-to-Objects because LiteDB's LINQ Count with status filter
                // doesn't reliably match on recently updated int fields
                var statusValue = (int)statusFilter.Value;
                return col.FindAll().Count(x => x.Status == statusValue);
            }
        }

        private static MessageHistoryRecord MapRecord(HistoryTable h)
        {
            return new MessageHistoryRecord
            {
                QueueId = h.QueueId,
                CorrelationId = h.CorrelationId,
                Status = (MessageHistoryStatus)h.Status,
                EnqueuedUtc = new DateTime(h.EnqueuedUtc, DateTimeKind.Utc),
                StartedUtc = h.StartedUtc > 0 ? new DateTime(h.StartedUtc, DateTimeKind.Utc) : (DateTime?)null,
                CompletedUtc = h.CompletedUtc > 0 ? new DateTime(h.CompletedUtc, DateTimeKind.Utc) : (DateTime?)null,
                DurationMs = h.CompletedUtc > 0 ? h.DurationMs : (long?)null,
                ExceptionText = h.ExceptionText,
                RetryCount = h.RetryCount,
                Route = h.Route,
                MessageType = h.MessageType,
                Body = h.Body,
                Headers = h.Headers
            };
        }
    }
}
