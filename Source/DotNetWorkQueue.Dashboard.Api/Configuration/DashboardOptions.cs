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

namespace DotNetWorkQueue.Dashboard.Api.Configuration
{
    /// <summary>
    /// Configuration options for the DotNetWorkQueue Dashboard API.
    /// </summary>
    public class DashboardOptions
    {
        /// <summary>
        /// Gets or sets whether Swagger/OpenAPI documentation is enabled.
        /// </summary>
        public bool EnableSwagger { get; set; } = true;

        /// <summary>
        /// Optional ASP.NET Core authorization policy name.
        /// If set, all dashboard controllers require this policy.
        /// If null, no authorization is applied (open access).
        /// </summary>
        public string AuthorizationPolicy { get; set; }

        /// <summary>
        /// Optional API key for simple authentication.
        /// When set, all dashboard endpoints require an <c>X-Api-Key</c> header matching this value.
        /// When null or empty, no API key check is performed.
        /// This is independent of <see cref="AuthorizationPolicy"/> — both can be used together.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Internal list of connection registrations.
        /// </summary>
        internal List<DashboardConnectionRegistration> ConnectionRegistrations { get; } = new List<DashboardConnectionRegistration>();

        /// <summary>
        /// Register a transport connection with its queues.
        /// </summary>
        /// <typeparam name="TTransportInit">The transport initialization type.</typeparam>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="configureQueues">Action to configure which queues to monitor on this connection.</param>
        public void AddConnection<TTransportInit>(
            string connectionString,
            Action<DashboardConnectionOptions> configureQueues)
            where TTransportInit : ITransportInit, new()
        {
            AddConnection<TTransportInit>(connectionString, null, configureQueues);
        }

        /// <summary>
        /// Register a transport connection with its queues and optional container configuration.
        /// </summary>
        /// <typeparam name="TTransportInit">The transport initialization type.</typeparam>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="containerConfig">Optional container configuration applied to all queues on this connection.</param>
        /// <param name="configureQueues">Action to configure which queues to monitor on this connection.</param>
        public void AddConnection<TTransportInit>(
            string connectionString,
            Action<IContainer> containerConfig,
            Action<DashboardConnectionOptions> configureQueues)
            where TTransportInit : ITransportInit, new()
        {
            var connectionOptions = new DashboardConnectionOptions();
            configureQueues(connectionOptions);

            ConnectionRegistrations.Add(new DashboardConnectionRegistration
            {
                TransportInitType = typeof(TTransportInit),
                ConnectionString = connectionString,
                DisplayName = connectionOptions.DisplayName
                    ?? $"Connection {ConnectionRegistrations.Count + 1}",
                ContainerConfig = containerConfig,
                Queues = connectionOptions.Queues
            });
        }
    }
}
