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
            return $"CREATE UNIQUE INDEX IX_QueueIDExceptionType{errorTrackingTable} " +
                   $"ON {errorTrackingTable} (QueueID, ExceptionType);";
        }
    }
}
