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
using System.Text;
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
        /// PostgreSQL truncates every identifier at 63 bytes, and index names are unique per schema, so
        /// the obvious name is not always usable: a 45 character queue name yields an 81 byte index
        /// name, and two queues whose names match for their first 40 characters truncate to the same
        /// identifier. Measured, not reasoned about - the second CREATE fails with 42P07.
        ///
        /// That matters more here than at creation. Both queues already exist - they were created
        /// before the index did, which is the only reason there is anything to upgrade - so the
        /// collision cannot be avoided by naming one of them differently, and the second queue would
        /// be left refusing producers with no way forward.
        ///
        /// A name that fits is left exactly as the creation path builds it, so the ordinary case stays
        /// recognisable. Only the long case is shortened, and it ends in a hash of the full table name
        /// so that two tables sharing a prefix do not share an index name. Nothing reads the name back
        /// - the index is found by its shape - so it only has to be unique and stable.
        /// </remarks>
        private static string IndexName(string errorTrackingTable)
        {
            const string prefix = "IX_QueueIDExceptionType";
            const int maxIdentifierBytes = 63;

            var full = string.Concat(prefix, errorTrackingTable);
            if (Encoding.UTF8.GetByteCount(full) <= maxIdentifierBytes)
                return full;

            //FNV-1a for the same reason SchemaUpgradeLockName uses it: string.GetHashCode is
            //randomised per process, so it would name the index differently on every run
            var suffix = "_" + Fnv1A(errorTrackingTable).ToString("x16");
            var room = maxIdentifierBytes - Encoding.UTF8.GetByteCount(prefix) - suffix.Length;

            //queue names are limited to alphanumerics, underscores and dots, so a character is a byte
            //here; Min guards the case of a prefix change leaving no room rather than a name that could
            //occur today
            var kept = errorTrackingTable.Substring(0, Math.Max(0, Math.Min(room, errorTrackingTable.Length)));
            return string.Concat(prefix, kept, suffix);
        }

        private static ulong Fnv1A(string value)
        {
            const ulong offsetBasis = 14695981039346656037;
            const ulong prime = 1099511628211;

            var hash = offsetBasis;
            foreach (var b in Encoding.UTF8.GetBytes(value))
            {
                hash ^= b;
                hash *= prime;
            }

            return hash;
        }
    }
}
