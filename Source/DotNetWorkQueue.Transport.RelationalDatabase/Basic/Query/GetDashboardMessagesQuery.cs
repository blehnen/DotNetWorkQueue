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
using System.Collections.Generic;
using DotNetWorkQueue.Transport.Shared;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query
{
    /// <summary>
    /// Dashboard query: gets a paged list of messages from the metadata table.
    /// </summary>
    public class GetDashboardMessagesQuery : IQuery<IReadOnlyList<DashboardMessage>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetDashboardMessagesQuery"/> class.
        /// </summary>
        /// <param name="pageIndex">The zero-based page index.</param>
        /// <param name="pageSize">The number of records per page.</param>
        /// <param name="statusFilter">Optional status value to filter by.</param>
        public GetDashboardMessagesQuery(int pageIndex, int pageSize, int? statusFilter)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
            StatusFilter = statusFilter;
        }

        /// <summary>
        /// Gets the zero-based page index.
        /// </summary>
        public int PageIndex { get; }

        /// <summary>
        /// Gets the number of records per page.
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Gets the optional status filter. Null for all statuses.
        /// </summary>
        public int? StatusFilter { get; }
    }
}
