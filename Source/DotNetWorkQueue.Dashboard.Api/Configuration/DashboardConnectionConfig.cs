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

namespace DotNetWorkQueue.Dashboard.Api.Configuration
{
    /// <summary>
    /// Represents a single transport connection entry in the Dashboard JSON configuration.
    /// </summary>
    public class DashboardConnectionConfig
    {
        /// <summary>
        /// The transport type name: "SqlServer", "PostgreSql", "SQLite", "LiteDb", or "Redis".
        /// </summary>
        public string Transport { get; set; } = string.Empty;

        /// <summary>
        /// The connection string for this transport.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Optional display name shown in the Dashboard UI. Defaults to the transport name.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Queue names to monitor on this connection.
        /// </summary>
        public string[] Queues { get; set; } = System.Array.Empty<string>();
    }
}
