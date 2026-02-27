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

namespace DotNetWorkQueue.Dashboard.Api.Configuration
{
    /// <summary>
    /// Options for configuring queues on a single connection.
    /// </summary>
    public class DashboardConnectionOptions
    {
        /// <summary>
        /// Gets or sets a human-readable display name for this connection.
        /// Used in API responses instead of the connection string.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets the list of queue options for this connection.
        /// </summary>
        public List<DashboardQueueOptions> Queues { get; } = new List<DashboardQueueOptions>();

        /// <summary>
        /// Add a queue to monitor with no interceptor overrides.
        /// </summary>
        /// <param name="queueName">The name of the queue.</param>
        public void AddQueue(string queueName)
        {
            Queues.Add(new DashboardQueueOptions { QueueName = queueName });
        }

        /// <summary>
        /// Add a queue to monitor with per-queue container configuration for interceptors.
        /// </summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <param name="configureInterceptors">Container configuration delegate for registering interceptors.</param>
        public void AddQueue(string queueName, Action<IContainer> configureInterceptors)
        {
            Queues.Add(new DashboardQueueOptions
            {
                QueueName = queueName,
                InterceptorConfiguration = configureInterceptors
            });
        }
    }
}
