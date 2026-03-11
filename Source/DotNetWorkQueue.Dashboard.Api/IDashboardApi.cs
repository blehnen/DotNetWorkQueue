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

namespace DotNetWorkQueue.Dashboard.Api
{
    /// <summary>
    /// Core dashboard API providing access to registered connections and queues.
    /// </summary>
    public interface IDashboardApi : IDisposable
    {
        /// <summary>
        /// Gets the registered connections.
        /// </summary>
        IReadOnlyDictionary<Guid, DashboardConnectionInfo> Connections { get; }

        /// <summary>
        /// Finds a queue by its unique identifier.
        /// </summary>
        /// <param name="queueId">The queue identifier.</param>
        /// <returns>The queue info, or null if not found.</returns>
        DashboardQueueInfo FindQueue(Guid queueId);

        /// <summary>
        /// Gets the internal container for a queue, allowing resolution of query handlers.
        /// The container is created lazily and cached per queue.
        /// </summary>
        /// <param name="queueId">The queue identifier.</param>
        /// <returns>The internal container for the specified queue.</returns>
        IContainer GetQueueContainer(Guid queueId);

        /// <summary>
        /// Gets the maintenance service for a queue, if the dashboard is hosting maintenance for it.
        /// </summary>
        /// <param name="queueId">The queue identifier.</param>
        /// <returns>The maintenance service, or null if not hosting maintenance for this queue.</returns>
        IQueueMaintenanceService GetMaintenanceService(Guid queueId);
    }
}
