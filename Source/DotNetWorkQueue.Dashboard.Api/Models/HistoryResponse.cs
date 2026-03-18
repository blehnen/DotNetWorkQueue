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

namespace DotNetWorkQueue.Dashboard.Api.Models
{
    /// <summary>
    /// Response model for a single message history record.
    /// </summary>
    public class HistoryResponse
    {
        /// <summary>The message's queue ID.</summary>
        public string QueueId { get; set; }

        /// <summary>Correlation ID for cross-system tracing.</summary>
        public string CorrelationId { get; set; }

        /// <summary>Current status (0=Enqueued, 1=Processing, 2=Complete, 3=Error, 4=Deleted, 5=Expired).</summary>
        public int Status { get; set; }

        /// <summary>When the message was enqueued.</summary>
        public DateTime EnqueuedUtc { get; set; }

        /// <summary>When processing started.</summary>
        public DateTime? StartedUtc { get; set; }

        /// <summary>When processing completed.</summary>
        public DateTime? CompletedUtc { get; set; }

        /// <summary>Processing duration in milliseconds.</summary>
        public long? DurationMs { get; set; }

        /// <summary>Truncated exception text for error status.</summary>
        public string ExceptionText { get; set; }

        /// <summary>Number of retries before final status.</summary>
        public int RetryCount { get; set; }

        /// <summary>Message route.</summary>
        public string Route { get; set; }

        /// <summary>Message body type name.</summary>
        public string MessageType { get; set; }
    }
}
