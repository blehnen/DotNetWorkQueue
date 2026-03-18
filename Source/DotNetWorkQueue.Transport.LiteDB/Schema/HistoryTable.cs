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
using DotNetWorkQueue.Transport.LiteDb.Basic;

namespace DotNetWorkQueue.Transport.LiteDb.Schema
{
    /// <summary>
    /// Message history table for LiteDB
    /// </summary>
    public class HistoryTable : ITable
    {
        /// <inheritdoc />
        public bool Create(LiteDbConnectionManager connection, LiteDbMessageQueueTransportOptions options,
            TableNameHelper helper)
        {
            using (var db = connection.GetDatabase())
            {
                var col = db.Database.GetCollection<HistoryTable>(helper.HistoryName);

                col.EnsureIndex(x => x.Id);
                col.EnsureIndex(x => x.QueueId, false);
                col.EnsureIndex(x => x.Status, false);

                return true;
            }
        }

        /// <summary>Auto-generated LiteDB ID.</summary>
        public int Id { get; set; }
        /// <summary>The message's queue ID.</summary>
        public string QueueId { get; set; }
        /// <summary>Correlation ID for cross-system tracing.</summary>
        public string CorrelationId { get; set; }
        /// <summary>Current status (maps to MessageHistoryStatus enum).</summary>
        public int Status { get; set; }
        /// <summary>When the message was enqueued (UTC ticks).</summary>
        public long EnqueuedUtc { get; set; }
        /// <summary>When processing started (UTC ticks). 0 if not started.</summary>
        public long StartedUtc { get; set; }
        /// <summary>When processing completed (UTC ticks). 0 if not completed.</summary>
        public long CompletedUtc { get; set; }
        /// <summary>Duration in milliseconds. 0 if not completed.</summary>
        public long DurationMs { get; set; }
        /// <summary>Truncated exception text for error status.</summary>
        public string ExceptionText { get; set; }
        /// <summary>Number of retries before final status.</summary>
        public int RetryCount { get; set; }
        /// <summary>Message route, if routing is enabled.</summary>
        public string Route { get; set; }
        /// <summary>Body type name from header.</summary>
        public string MessageType { get; set; }
        /// <summary>Serialized body bytes (only when StoreBody is enabled).</summary>
        public byte[] Body { get; set; }
        /// <summary>Serialized header bytes (only when StoreBody is enabled).</summary>
        public byte[] Headers { get; set; }
    }
}
