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
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.Schema
{
    /// <summary>
    /// Applies schema upgrades to a PostgreSQL queue.
    /// </summary>
    /// <remarks>
    /// Version 1 is the unique index on the error tracking table's (QueueID, ExceptionType), which
    /// queues created before #299 do not have. See <see cref="AErrorTrackingUniqueIndexVersion"/>.
    ///
    /// Version 2 converts the history and metadata timestamps to timestamptz, which queues created
    /// before #311 still hold as a naive timestamp. See <see cref="PostgreSqlTimestampTzVersion"/>.
    /// This transport therefore runs ahead of the others, which have only version 1 - version numbers
    /// are per transport, because the schemas are.
    /// </remarks>
    public class PostgreSqlSchemaUpdater : ASchemaUpdater
    {
        private readonly CommandStringCache _commandCache;
        //kept here as well as in the base: version 2 needs the declared source time zone,
        //and the base exposes the connection information to nothing
        private readonly IConnectionInformation _connectionInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlSchemaUpdater"/> class.
        /// </summary>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableNameHelper">The table names for this queue.</param>
        /// <param name="tableProbe">Answers whether a table exists.</param>
        /// <param name="upgradeLock">Stops two processes upgrading at once.</param>
        /// <param name="commandCache">The command cache, which a version uses to ask what the schema already looks like.</param>
        /// <param name="logger">The logger.</param>
        public PostgreSqlSchemaUpdater(IDbConnectionFactory connectionFactory,
            ITransactionFactory transactionFactory,
            IConnectionInformation connectionInformation,
            ITableNameHelper tableNameHelper,
            ISchemaTableProbe tableProbe,
            ISchemaUpgradeLock upgradeLock,
            CommandStringCache commandCache,
            ILogger logger)
            : base(connectionFactory, transactionFactory, connectionInformation, tableNameHelper,
                tableProbe, upgradeLock, logger)
        {
            Guard.NotNull(commandCache);
            Guard.NotNull(connectionInformation);
            _commandCache = commandCache;
            _connectionInformation = connectionInformation;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Add versions here, lowest first and never renumbered. A shipped version is immutable: a
        /// queue that already applied it will not apply it again, so editing one changes what that
        /// queue believes about itself and nothing will say so.
        /// </remarks>
        protected override void LoadVersions()
        {
            Versions.Add(1, new PostgreSqlErrorTrackingUniqueIndexVersion(_commandCache));
            Versions.Add(2, new PostgreSqlTimestampTzVersion(_connectionInformation));
        }

        /// <inheritdoc />
        protected override string CreateVersionTableScript(string tableName)
        {
            return $@"CREATE TABLE IF NOT EXISTS {tableName}
                      (
                          Id int NOT NULL PRIMARY KEY CHECK (Id = 1),
                          Version bigint NOT NULL,
                          LastUpdated timestamptz NOT NULL
                      );";
        }

        /// <inheritdoc />
        protected override string WriteVersionScript(string tableName, string versionParameterName)
        {
            return $@"INSERT INTO {tableName} (Id, Version, LastUpdated)
                      VALUES (1, {versionParameterName}, now())
                      ON CONFLICT (Id) DO UPDATE
                          SET Version = EXCLUDED.Version, LastUpdated = EXCLUDED.LastUpdated;";
        }
    }
}
