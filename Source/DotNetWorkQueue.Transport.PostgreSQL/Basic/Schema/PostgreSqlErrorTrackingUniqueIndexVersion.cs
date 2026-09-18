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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.Schema
{
    /// <inheritdoc />
    public class PostgreSqlErrorTrackingUniqueIndexVersion : AErrorTrackingUniqueIndexVersion
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlErrorTrackingUniqueIndexVersion"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        public PostgreSqlErrorTrackingUniqueIndexVersion(CommandStringCache commandCache)
            : base(commandCache)
        {
        }

        /// <inheritdoc />
        /// <remarks>
        /// The table name is appended, matching what PostgreSqlMessageQueueSchema builds for a new
        /// queue: index names are unique per schema in PostgreSQL rather than per table, so two queues
        /// in one database would otherwise collide.
        ///
        /// Unquoted, so PostgreSQL folds it to lower case exactly as the creation path's does. Nothing
        /// looks the index up by name - the check is on its shape - so the folding costs nothing here.
        /// </remarks>
        protected override string CreateUniqueIndexScript(string errorTrackingTable)
        {
            return $"CREATE UNIQUE INDEX {IndexName(errorTrackingTable)} " +
                   $"ON {errorTrackingTable} (QueueID, ExceptionType);";
        }

        /// <summary>
        /// The index name, shortened only when the full one would not survive PostgreSQL's identifier
        /// limit.
        /// </summary>
        /// <param name="errorTrackingTable">The error tracking table.</param>
        /// <remarks>
        /// Shared with the creation path through <see cref="PostgreSqlIdentifier"/>, which is safe
        /// because only the <em>name</em> is shared. The index definition stays here, where a shipped
        /// version can keep producing what it produced the day it shipped. Nothing reads the name back
        /// - the index is found by its shape - so even if the helper changed, a queue that had already
        /// applied this version would be unaffected.
        ///
        /// Why it is needed at all: a 45 character queue name yields an 81 byte index name, PostgreSQL
        /// truncates at 63, and two queues whose names match for their first 40 characters collapse to
        /// one identifier. Measured, not reasoned about - the second CREATE fails with 42P07. It
        /// matters more here than at creation, because both queues already exist and neither can be
        /// renamed out of the way (GitHub #308, #375).
        /// </remarks>
        private static string IndexName(string errorTrackingTable) =>
            PostgreSqlIdentifier.Shorten($"IX_QueueIDExceptionType{errorTrackingTable}");
    }
}
