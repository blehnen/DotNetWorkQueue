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
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Queries message history records.
    /// </summary>
    public interface IQueryMessageHistory
    {
        /// <summary>
        /// Gets a paged list of history records, optionally filtered by status.
        /// </summary>
        /// <param name="pageIndex">Zero-based page index.</param>
        /// <param name="pageSize">Number of records per page.</param>
        /// <param name="statusFilter">Optional status filter. Null returns all statuses.</param>
        /// <returns>A list of history records ordered by EnqueuedUtc descending.</returns>
        IReadOnlyList<MessageHistoryRecord> Get(int pageIndex, int pageSize, MessageHistoryStatus? statusFilter);

        /// <summary>
        /// Gets the history record for a specific message.
        /// </summary>
        /// <param name="queueId">The message's queue ID.</param>
        /// <returns>The history record, or null if not found.</returns>
        MessageHistoryRecord GetByQueueId(string queueId);

        /// <summary>
        /// Gets the total count of history records, optionally filtered by status.
        /// </summary>
        /// <param name="statusFilter">Optional status filter. Null counts all records.</param>
        /// <returns>The total count.</returns>
        long GetCount(MessageHistoryStatus? statusFilter);
    }
}
