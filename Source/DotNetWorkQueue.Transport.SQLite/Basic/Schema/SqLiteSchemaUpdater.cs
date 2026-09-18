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
using System.Data.Common;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Transport.SQLite.Basic.Schema
{
    /// <summary>
    /// Applies schema upgrades to a SQLite queue.
    /// </summary>
    /// <remarks>
    /// Version 1 is the unique index on the error tracking table's (QueueID, ExceptionType), which
    /// queues created before #299 do not have. See <see cref="AErrorTrackingUniqueIndexVersion"/>.
    /// </remarks>
    public class SqLiteSchemaUpdater : ASchemaUpdater
    {
        private readonly CommandStringCache _commandCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqLiteSchemaUpdater"/> class.
        /// </summary>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableNameHelper">The table names for this queue.</param>
        /// <param name="tableProbe">Answers whether a table exists.</param>
        /// <param name="upgradeLock">Stops two processes upgrading at once.</param>
        /// <param name="commandCache">The command cache, which a version uses to ask what the schema already looks like.</param>
        /// <param name="logger">The logger.</param>
        public SqLiteSchemaUpdater(IDbConnectionFactory connectionFactory,
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
            _commandCache = commandCache;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Add versions here, lowest first and never renumbered. A shipped version is immutable: a
        /// queue that already applied it will not apply it again, so editing one changes what that
        /// queue believes about itself and nothing will say so.
        /// </remarks>
        protected override void LoadVersions()
        {
            Versions.Add(1, new SqLiteErrorTrackingUniqueIndexVersion(_commandCache));
        }

        /// <inheritdoc />
        /// <remarks>
        /// Serializable explicitly, which System.Data.SQLite turns into BEGIN IMMEDIATE, so the writer
        /// lock is held from the first statement rather than taken at the first write.
        ///
        /// This is what makes SqLiteSchemaUpgradeLock's answer honest. Without it the isolation level
        /// comes from the connection, and a connection string carrying
        /// "Default IsolationLevel=ReadCommitted" begins deferred: two upgrades would then both read
        /// the version before either wrote, and the loser would fail busy rather than wait and find
        /// the work done. Nothing would be applied twice either way, but the behaviour would depend on
        /// a setting nothing here controls.
        /// </remarks>
        protected override DbTransaction BeginUpgradeTransaction(DbConnection connection) =>
            connection.BeginTransaction(IsolationLevel.Serializable);

        /// <inheritdoc />
        protected override string CreateVersionTableScript(string tableName)
        {
            return $@"CREATE TABLE IF NOT EXISTS {tableName}
                      (
                          Id integer NOT NULL PRIMARY KEY CHECK (Id = 1),
                          Version integer NOT NULL,
                          LastUpdated text NOT NULL
                      );";
        }

        /// <inheritdoc />
        protected override string WriteVersionScript(string tableName, string versionParameterName)
        {
            return $@"INSERT INTO {tableName} (Id, Version, LastUpdated)
                      VALUES (1, {versionParameterName}, datetime('now'))
                      ON CONFLICT (Id) DO UPDATE
                          SET Version = excluded.Version, LastUpdated = excluded.LastUpdated;";
        }
    }
}
