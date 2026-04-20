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
using System.Collections.Generic;
using System.Linq;
using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue.Transport.Memory.Basic
{
    /// <summary>
    /// Queries message history records for the in-memory transport.
    /// </summary>
    public class QueryMessageHistoryHandler : IQueryMessageHistory
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly IBaseTransportOptions _options;

        /// <inheritdoc />
        public QueryMessageHistoryHandler(IConnectionInformation connectionInformation, IBaseTransportOptions options)
        {
            _connectionInformation = connectionInformation;
            _options = options;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Does not check <see cref="IBaseTransportOptions.EnableHistory"/>. That flag gates
        /// WRITES only; reads return whatever is in the in-memory store. Missing store → empty.
        /// </remarks>
        public IReadOnlyList<MessageHistoryRecord> Get(int pageIndex, int pageSize, MessageHistoryStatus? statusFilter)
        {
            var records = GetRecords();
            if (records == null) return new List<MessageHistoryRecord>();
            var query = records.Values.AsEnumerable();
            if (statusFilter.HasValue) query = query.Where(r => r.Status == statusFilter.Value);
            return query.OrderByDescending(r => r.EnqueuedUtc).Skip(pageIndex * pageSize).Take(pageSize).ToList();
        }

        /// <inheritdoc />
        public MessageHistoryRecord GetByQueueId(string queueId)
        {
            var records = GetRecords();
            if (records == null) return null;
            records.TryGetValue(queueId, out var record);
            return record;
        }

        /// <inheritdoc />
        public long GetCount(MessageHistoryStatus? statusFilter)
        {
            var records = GetRecords();
            if (records == null) return 0;
            return statusFilter.HasValue ? records.Values.Count(r => r.Status == statusFilter.Value) : records.Count;
        }

        private System.Collections.Concurrent.ConcurrentDictionary<string, MessageHistoryRecord> GetRecords()
        {
            var key = $"{_connectionInformation.QueueName}|{_connectionInformation.ConnectionString}";
            return WriteMessageHistoryHandler.GetRecordsForQueue(key);
        }
    }
}
