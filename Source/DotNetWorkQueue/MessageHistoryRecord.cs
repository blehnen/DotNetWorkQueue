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

namespace DotNetWorkQueue
{
    /// <summary>
    /// Represents a single message history record.
    /// </summary>
    public class MessageHistoryRecord
    {
        /// <summary>
        /// The message's queue ID (same as used by the dashboard).
        /// </summary>
        public string QueueId { get; set; }

        /// <summary>
        /// Correlation ID from message headers, for cross-system tracing.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Current status of the message.
        /// </summary>
        public MessageHistoryStatus Status { get; set; }

        /// <summary>
        /// When the message was added to the queue.
        /// </summary>
        public DateTime EnqueuedUtc { get; set; }

        /// <summary>
        /// When processing began. Null if never dequeued.
        /// </summary>
        public DateTime? StartedUtc { get; set; }

        /// <summary>
        /// When processing finished (success, error, or delete). Null if not completed.
        /// </summary>
        public DateTime? CompletedUtc { get; set; }

        /// <summary>
        /// Duration in milliseconds (CompletedUtc - StartedUtc). Null if not completed.
        /// </summary>
        public long? DurationMs { get; set; }

        /// <summary>
        /// Truncated exception text for error status. Null otherwise.
        /// </summary>
        public string ExceptionText { get; set; }

        /// <summary>
        /// Number of times this message was retried before reaching its final status.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Message route, if routing is enabled.
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// Body type name from the Queue-MessageBodyType header, if available.
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// Serialized message body bytes. Only populated when StoreBody is enabled.
        /// </summary>
        public byte[] Body { get; set; }

        /// <summary>
        /// Serialized message headers bytes. Only populated when StoreBody is enabled.
        /// </summary>
        public byte[] Headers { get; set; }
    }
}
