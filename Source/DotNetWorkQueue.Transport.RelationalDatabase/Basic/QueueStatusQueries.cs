// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// Contains all of the support status queries
    /// </summary>
    public class QueueStatusQueries
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueStatusQueries"/> class.
        /// </summary>
        /// <param name="pendingQueryHandler">The pending query handler.</param>
        /// <param name="pendingExcludeDelayQueryHandler">The pending exclude delay query handler.</param>
        /// <param name="pendingDelayedQueryHandler">The pending delayed query handler.</param>
        /// <param name="workingQueryHandler">The working query handler.</param>
        /// <param name="errorQueryHandler">The error query handler.</param>
        public QueueStatusQueries(
            IQueryHandler<GetPendingCountQuery, long> pendingQueryHandler,
            IQueryHandler<GetPendingExcludeDelayCountQuery, long> pendingExcludeDelayQueryHandler,
            IQueryHandler<GetPendingDelayedCountQuery, long> pendingDelayedQueryHandler,
            IQueryHandler<GetWorkingCountQuery, long> workingQueryHandler,
            IQueryHandler<GetErrorCountQuery, long> errorQueryHandler)
        {
            PendingQueryHandler = pendingQueryHandler;
            PendingExcludeDelayQueryHandler = pendingExcludeDelayQueryHandler;
            PendingDelayedQueryHandler = pendingDelayedQueryHandler;
            WorkingQueryHandler = workingQueryHandler;
            ErrorQueryHandler = errorQueryHandler;
        }

        /// <summary>
        /// Gets the pending query handler.
        /// </summary>
        /// <value>
        /// The pending query handler.
        /// </value>
        public IQueryHandler<GetPendingCountQuery, long> PendingQueryHandler { get;}
        /// <summary>
        /// Gets the pending exclude delay query handler.
        /// </summary>
        /// <value>
        /// The pending exclude delay query handler.
        /// </value>
        public IQueryHandler<GetPendingExcludeDelayCountQuery, long> PendingExcludeDelayQueryHandler { get; }
        /// <summary>
        /// Gets the pending delayed query handler.
        /// </summary>
        /// <value>
        /// The pending delayed query handler.
        /// </value>
        public IQueryHandler<GetPendingDelayedCountQuery, long> PendingDelayedQueryHandler { get;  }
        /// <summary>
        /// Gets the working query handler.
        /// </summary>
        /// <value>
        /// The working query handler.
        /// </value>
        public IQueryHandler<GetWorkingCountQuery, long> WorkingQueryHandler { get;  }
        /// <summary>
        /// Gets the error query handler.
        /// </summary>
        /// <value>
        /// The error query handler.
        /// </value>
        public IQueryHandler<GetErrorCountQuery, long> ErrorQueryHandler { get; }
    }
}
