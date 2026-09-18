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

namespace DotNetWorkQueue.Transport.SqlServer.Basic.Schema
{
    /// <inheritdoc />
    public class SqlServerErrorTrackingUniqueIndexVersion : AErrorTrackingUniqueIndexVersion
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerErrorTrackingUniqueIndexVersion"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        public SqlServerErrorTrackingUniqueIndexVersion(CommandStringCache commandCache)
            : base(commandCache)
        {
        }

        /// <inheritdoc />
        /// <remarks>
        /// The name carries no table in it, matching what SQLServerMessageQueueSchema builds for a new
        /// queue: SQL Server scopes index names to their table, and appending the table pushed the
        /// identifier past the 128 character limit for the queue names the validator allows.
        ///
        /// The table arrives schema-qualified here - ITableNameHelper is SqlServerTableNameHelper, which
        /// prefixes the schema - so it is written unbracketed, as the rest of this transport's SQL does.
        /// Bracketing it whole would name one object with a dot in it rather than a table in a schema.
        /// </remarks>
        protected override string CreateUniqueIndexScript(string errorTrackingTable)
        {
            return $"CREATE UNIQUE NONCLUSTERED INDEX [IX_QueueIDExceptionType] " +
                   $"ON {errorTrackingTable} ([QueueID], [ExceptionType]);";
        }
    }
}
