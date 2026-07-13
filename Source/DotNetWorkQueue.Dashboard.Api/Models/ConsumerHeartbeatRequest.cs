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
    /// Request model for sending a consumer heartbeat.
    /// </summary>
    /// <remarks>
    /// Value-type members are nullable so the model binder can distinguish an omitted field
    /// from a posted default; the controller coalesces each to its prior default, preserving
    /// the released wire behavior (a missing counter counts as zero).
    /// </remarks>
    public class ConsumerHeartbeatRequest
    {
        /// <summary>Gets or sets the consumer identifier returned from registration.</summary>
        public Guid? ConsumerId { get; set; }

        /// <summary>Gets or sets the running total of successfully processed messages since consumer start.</summary>
        public long? MessagesProcessed { get; set; }

        /// <summary>Gets or sets the running total of messages that threw exceptions since consumer start.</summary>
        public long? MessagesErrored { get; set; }

        /// <summary>Gets or sets the running total of messages rolled back since consumer start.</summary>
        public long? MessagesRolledBack { get; set; }

        /// <summary>Gets or sets the running total of poison messages since consumer start.</summary>
        public long? PoisonMessages { get; set; }
    }
}
