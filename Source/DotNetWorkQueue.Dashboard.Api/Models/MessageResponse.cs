// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
    /// Response model for a queue message.
    /// </summary>
    public class MessageResponse
    {
        /// <summary>Gets or sets the queue identifier (primary key).</summary>
        public long QueueId { get; set; }

        /// <summary>Gets or sets the date/time the message was queued.</summary>
        public DateTimeOffset? QueuedDateTime { get; set; }

        /// <summary>Gets or sets the correlation identifier.</summary>
        public string CorrelationId { get; set; }

        /// <summary>Gets or sets the message status. Null if status tracking is not enabled.</summary>
        public int? Status { get; set; }

        /// <summary>Gets or sets the message priority. Null if priority is not enabled.</summary>
        public int? Priority { get; set; }

        /// <summary>Gets or sets the scheduled process time. Null if delayed processing is not enabled.</summary>
        public DateTimeOffset? QueueProcessTime { get; set; }

        /// <summary>Gets or sets the last heartbeat timestamp. Null if heartbeat is not enabled.</summary>
        public DateTimeOffset? HeartBeat { get; set; }

        /// <summary>Gets or sets the expiration time. Null if message expiration is not enabled.</summary>
        public DateTimeOffset? ExpirationTime { get; set; }

        /// <summary>Gets or sets the message route. Null if routing is not enabled.</summary>
        public string Route { get; set; }
    }
}
