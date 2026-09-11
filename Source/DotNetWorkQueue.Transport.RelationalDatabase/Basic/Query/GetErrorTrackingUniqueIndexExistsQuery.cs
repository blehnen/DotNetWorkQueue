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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query
{
    /// <summary>
    /// Whether the unique index on the error tracking table's (QueueID, ExceptionType) is present.
    /// </summary>
    /// <remarks>
    /// Queues created before that index existed do not have it, and the library does not upgrade
    /// schemas - a user re-creates the queue instead. So the error count write asks once, and falls back
    /// to check-then-write where the index is missing rather than failing on a statement the older
    /// schema cannot support.
    /// </remarks>
    public class GetErrorTrackingUniqueIndexExistsQuery : IQuery<bool>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetErrorTrackingUniqueIndexExistsQuery"/> class.
        /// </summary>
        /// <param name="tableName">The error tracking table.</param>
        /// <param name="indexName">The index to look for.</param>
        public GetErrorTrackingUniqueIndexExistsQuery(string tableName, string indexName)
        {
            TableName = tableName;
            IndexName = indexName;
        }

        /// <summary>
        /// The error tracking table.
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// The index to look for.
        /// </summary>
        public string IndexName { get; }
    }
}
