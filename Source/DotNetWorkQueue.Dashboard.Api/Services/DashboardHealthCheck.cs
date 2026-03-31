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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DotNetWorkQueue.Dashboard.Api.Services
{
    /// <summary>
    /// Health check that verifies the Dashboard API service is alive and reports uptime.
    /// </summary>
    internal class DashboardHealthCheck : IHealthCheck
    {
        private static readonly DateTime StartTime = DateTime.UtcNow;
        private readonly IDashboardApi _dashboardApi;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardHealthCheck"/> class.
        /// </summary>
        /// <param name="dashboardApi">The dashboard API instance.</param>
        public DashboardHealthCheck(IDashboardApi dashboardApi)
        {
            _dashboardApi = dashboardApi;
        }

        /// <inheritdoc />
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Verify the service is accessible (not disposed)
                var connections = _dashboardApi.Connections;

                var data = new Dictionary<string, object>
                {
                    { "status", "Healthy" },
                    { "uptime", (DateTime.UtcNow - StartTime).ToString() },
                    { "connections", connections.Count }
                };

                return Task.FromResult(HealthCheckResult.Healthy("Dashboard API is running", data));
            }
            catch (ObjectDisposedException)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Dashboard API service has been disposed"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Dashboard API health check failed", ex));
            }
        }
    }
}
