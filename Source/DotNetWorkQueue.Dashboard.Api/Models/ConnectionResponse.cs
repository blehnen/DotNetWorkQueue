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
    /// Response model for a registered connection.
    /// </summary>
    public class ConnectionResponse
    {
        /// <summary>Gets or sets the connection identifier.</summary>
        public Guid Id { get; set; }

        /// <summary>Gets or sets the human-readable display name for this connection.</summary>
        public string DisplayName { get; set; }

        /// <summary>Gets or sets the number of queues on this connection.</summary>
        public int QueueCount { get; set; }
    }
}
