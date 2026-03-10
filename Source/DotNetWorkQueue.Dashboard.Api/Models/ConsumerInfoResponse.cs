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
    /// Response model containing full consumer information for the UI.
    /// </summary>
    public class ConsumerInfoResponse
    {
        /// <summary>Gets or sets the unique consumer identifier.</summary>
        public Guid ConsumerId { get; set; }

        /// <summary>Gets or sets the queue name the consumer is processing.</summary>
        public string QueueName { get; set; }

        /// <summary>Gets or sets the connection string for the queue.</summary>
        public string ConnectionString { get; set; }

        /// <summary>Gets or sets the machine name where the consumer is running.</summary>
        public string MachineName { get; set; }

        /// <summary>Gets or sets the process ID of the consumer.</summary>
        public int ProcessId { get; set; }

        /// <summary>Gets or sets the optional friendly name for this consumer instance.</summary>
        public string FriendlyName { get; set; }

        /// <summary>Gets or sets the time the consumer registered.</summary>
        public DateTimeOffset RegisteredAt { get; set; }

        /// <summary>Gets or sets the time of the last heartbeat.</summary>
        public DateTimeOffset LastHeartbeat { get; set; }

        /// <summary>Gets or sets the matched dashboard queue identifier, if any.</summary>
        public Guid? MatchedQueueId { get; set; }
    }
}
