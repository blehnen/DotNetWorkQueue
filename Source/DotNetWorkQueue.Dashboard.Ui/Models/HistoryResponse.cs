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

namespace DotNetWorkQueue.Dashboard.Ui.Models
{
    public class HistoryResponse
    {
        public string? QueueId { get; set; }
        public string? CorrelationId { get; set; }
        public int Status { get; set; }
        public DateTime EnqueuedUtc { get; set; }
        public DateTime? StartedUtc { get; set; }
        public DateTime? CompletedUtc { get; set; }
        public long? DurationMs { get; set; }
        public string? ExceptionText { get; set; }
        public int RetryCount { get; set; }
        public string? Route { get; set; }
        public string? MessageType { get; set; }

        public string StatusName => Status switch
        {
            0 => "Enqueued",
            1 => "Processing",
            2 => "Complete",
            3 => "Error",
            4 => "Deleted",
            5 => "Expired",
            _ => "Unknown"
        };
    }
}
