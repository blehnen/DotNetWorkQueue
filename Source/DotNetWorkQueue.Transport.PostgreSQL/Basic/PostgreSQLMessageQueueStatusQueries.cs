// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Query;
namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    /// Contains all of the support status queries
    /// </summary>
    internal class PostgreSqlMessageQueueStatusQueries
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlMessageQueueStatusQueries"/> class.
        /// </summary>
        /// <param name="pendingQueryHandler">The pending query handler.</param>
        /// <param name="pendingExcludeDelayQueryHandler">The pending exclude delay query handler.</param>
        /// <param name="pendingDelayedQueryHandler">The pending delayed query handler.</param>
        /// <param name="workingQueryHandler">The working query handler.</param>
        /// <param name="errorQueryHandler">The error query handler.</param>
        public PostgreSqlMessageQueueStatusQueries(
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

        public IQueryHandler<GetPendingCountQuery, long> PendingQueryHandler { get; private set; }
        public IQueryHandler<GetPendingExcludeDelayCountQuery, long> PendingExcludeDelayQueryHandler { get; private set; }
        public IQueryHandler<GetPendingDelayedCountQuery, long> PendingDelayedQueryHandler { get; private set; }
        public IQueryHandler<GetWorkingCountQuery, long> WorkingQueryHandler { get; private set; }
        public IQueryHandler<GetErrorCountQuery, long> ErrorQueryHandler { get; private set; }
    }
}
