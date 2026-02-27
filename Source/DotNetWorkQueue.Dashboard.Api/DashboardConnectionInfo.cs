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
using System.Collections.Generic;

namespace DotNetWorkQueue.Dashboard.Api
{
    /// <summary>
    /// Represents a registered transport connection with its queues.
    /// </summary>
    public class DashboardConnectionInfo
    {
        /// <summary>
        /// Gets the unique identifier for this connection.
        /// </summary>
        public Guid Id { get; internal set; }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        public string ConnectionString { get; internal set; }

        /// <summary>
        /// Gets the human-readable display name for this connection.
        /// </summary>
        public string DisplayName { get; internal set; }

        /// <summary>
        /// Gets the queues on this connection.
        /// </summary>
        public IReadOnlyList<DashboardQueueInfo> Queues { get; internal set; }
    }
}
