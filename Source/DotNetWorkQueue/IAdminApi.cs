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
using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Admin API wrapper for admin functions per-queue
    /// </summary>
    public interface IAdminApi : IDisposable, IIsDisposed
    {
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        AdminApiConfiguration Configuration { get; }

        /// <summary>
        /// Current configured connections
        /// </summary>
        IReadOnlyDictionary<Guid, Tuple<IQueueContainer, QueueConnection>> Connections { get; }

        /// <summary>
        /// Adds a new connection
        /// </summary>
        /// <param name="container"></param>
        /// <param name="connection"></param>
        /// <returns>An Id for the connection, used in query calls</returns>
        Guid AddQueueConnection(IQueueContainer container, QueueConnection connection);

        /// <summary>
        /// returns the count of items in the queue
        /// </summary>
        /// <param name="id"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        long? Count(Guid id, QueueStatusAdmin? status);
    }
}
