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

namespace DotNetWorkQueue.Dashboard.Api.Configuration
{
    /// <summary>
    /// Options for a single queue in the dashboard.
    /// </summary>
    public class DashboardQueueOptions
    {
        /// <summary>
        /// Gets or sets the name of the queue.
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Optional per-queue container configuration delegate.
        /// Used to register interceptors using the same pattern as QueueContainer.
        /// Takes highest priority when resolving interceptor configuration.
        /// </summary>
        public Action<IContainer> InterceptorConfiguration { get; set; }

        /// <summary>
        /// Optional name of a registered interceptor profile.
        /// Profiles are registered via <see cref="DashboardOptions.AddInterceptorProfile"/>.
        /// Used when <see cref="InterceptorConfiguration"/> is null.
        /// </summary>
        public string InterceptorProfile { get; set; }

        /// <summary>
        /// Optional JSON-bindable interceptor options for built-in interceptors (GZip, TripleDES).
        /// Used when both <see cref="InterceptorConfiguration"/> and <see cref="InterceptorProfile"/> are null.
        /// </summary>
        public DashboardInterceptorOptions Interceptors { get; set; }

        /// <summary>
        /// When true, the dashboard will run maintenance monitors (heartbeat reset, expiration cleanup,
        /// error cleanup) for this queue. The corresponding consumer should set
        /// <c>MaintenanceMode = External</c> to avoid duplicate work.
        /// </summary>
        public bool HostMaintenance { get; set; }
    }
}
