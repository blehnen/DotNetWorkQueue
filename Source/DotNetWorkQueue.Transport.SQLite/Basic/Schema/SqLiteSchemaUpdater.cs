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
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Transport.SQLite.Basic.Schema
{
    /// <summary>
    /// Applies schema upgrades to a SQLite queue.
    /// </summary>
    /// <remarks>
    /// <see cref="LoadVersions"/> is empty, so the target version is zero and every queue is already
    /// current. The framework is in place and does nothing until the first version is added here
    /// (GitHub #308).
    /// </remarks>
    public class SqLiteSchemaUpdater : ASchemaUpdater
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqLiteSchemaUpdater"/> class.
        /// </summary>
        public SqLiteSchemaUpdater(IDbConnectionFactory connectionFactory,
            ITransactionFactory transactionFactory,
            IConnectionInformation connectionInformation,
            ITableNameHelper tableNameHelper,
            ISchemaTableProbe tableProbe,
            ISchemaUpgradeLock upgradeLock,
            ILogger logger)
            : base(connectionFactory, transactionFactory, connectionInformation, tableNameHelper,
                tableProbe, upgradeLock, logger)
        {
        }

        /// <inheritdoc />
        /// <remarks>
        /// Add versions here, lowest first and never renumbered. A shipped version is immutable: a
        /// queue that already applied it will not apply it again, so editing one changes what that
        /// queue believes about itself and nothing will say so.
        /// </remarks>
        protected override void LoadVersions()
        {
            //no versions yet - see the class remarks
        }

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
