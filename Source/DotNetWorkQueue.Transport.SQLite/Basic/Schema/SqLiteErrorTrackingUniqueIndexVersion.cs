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

namespace DotNetWorkQueue.Transport.SQLite.Basic.Schema
{
    /// <inheritdoc />
    public class SqLiteErrorTrackingUniqueIndexVersion : AErrorTrackingUniqueIndexVersion
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqLiteErrorTrackingUniqueIndexVersion"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        public SqLiteErrorTrackingUniqueIndexVersion(CommandStringCache commandCache)
            : base(commandCache)
        {
        }

        /// <inheritdoc />
        /// <remarks>
        /// The table name is appended here rather than by a script writer, which is what SQLite's
        /// Constraint.Script does for a new queue - so an upgraded queue ends up with the same index
        /// name as a created one. Index names are unique per database in SQLite, so the table has to be
        /// part of it.
        /// </remarks>
        protected override string CreateUniqueIndexScript(string errorTrackingTable)
        {
            return $"CREATE UNIQUE INDEX IX_QueueIDExceptionType{errorTrackingTable} " +
                   $"ON {errorTrackingTable} (QueueID, ExceptionType);";
        }
    }
}
