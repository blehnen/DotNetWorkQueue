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
using System.Linq;

namespace DotNetWorkQueue.Transport.Memory.Basic
{
    /// <summary>
    /// Purges old message history records for the in-memory transport.
    /// </summary>
    public class PurgeMessageHistoryHandler : IPurgeMessageHistory
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly IHistoryConfiguration _config;

        public PurgeMessageHistoryHandler(IConnectionInformation connectionInformation, IHistoryConfiguration config)
        {
            _connectionInformation = connectionInformation;
            _config = config;
        }

        /// <inheritdoc />
        public long Purge(DateTime olderThan)
        {
            if (!_config.Enabled) return 0;
            var key = $"{_connectionInformation.QueueName}|{_connectionInformation.ConnectionString}";
            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            if (records == null) return 0;

            var toRemove = records.Values
                .Where(r => (r.CompletedUtc.HasValue && r.CompletedUtc.Value < olderThan) ||
                            (!r.CompletedUtc.HasValue && r.EnqueuedUtc < olderThan))
                .Select(r => r.QueueId).ToList();

            long count = 0;
            foreach (var id in toRemove)
                if (records.TryRemove(id, out _)) count++;
            return count;
        }
    }
}
