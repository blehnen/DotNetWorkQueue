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
    /// Response model for queue maintenance service status.
    /// </summary>
    public class MaintenanceStatusResponse
    {
        /// <summary>
        /// Whether the dashboard is configured to host maintenance for this queue.
        /// </summary>
        public bool HostMaintenance { get; set; }

        /// <summary>
        /// Whether the maintenance service is currently running.
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// UTC timestamp of the last completed maintenance run, or null if no run has completed yet.
        /// </summary>
        public DateTime? LastRunUtc { get; set; }
    }
}
