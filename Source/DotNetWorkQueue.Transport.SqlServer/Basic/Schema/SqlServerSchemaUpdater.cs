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

namespace DotNetWorkQueue.Transport.SqlServer.Basic.Schema
{
    /// <summary>
    /// Applies schema upgrades to a SQL Server queue.
    /// </summary>
    /// <remarks>
    /// <see cref="LoadVersions"/> is empty, so the target version is zero and every queue is already
    /// current. The framework is in place and does nothing until the first version is added here
    /// (GitHub #308).
    /// </remarks>
    public class SqlServerSchemaUpdater : ASchemaUpdater
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerSchemaUpdater"/> class.
        /// </summary>
        public SqlServerSchemaUpdater(IDbConnectionFactory connectionFactory,
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
        /// <remarks>
        /// The primary key is left unnamed on purpose. Table names here are schema qualified -
        /// SqlServerTableNameHelper returns dbo.TheQueue - so a constraint named after the table
        /// would contain a dot and the batch fails with "Incorrect syntax near '.'". Constraint
        /// names are database scoped in SQL Server, so the generated name is unique regardless.
        /// </remarks>
        protected override string CreateVersionTableScript(string tableName)
        {
            return $@"IF OBJECT_ID('{tableName}', 'U') IS NULL
                      CREATE TABLE {tableName}
                      (
                          Id int NOT NULL PRIMARY KEY CHECK (Id = 1),
                          Version bigint NOT NULL,
                          LastUpdated datetime2(7) NOT NULL
                      );";
        }

        /// <inheritdoc />
        protected override string WriteVersionScript(string tableName, string versionParameterName)
        {
            return $@"UPDATE {tableName} SET Version = {versionParameterName}, LastUpdated = SYSUTCDATETIME() WHERE Id = 1;
                      IF @@ROWCOUNT = 0
                          INSERT INTO {tableName} (Id, Version, LastUpdated)
                          VALUES (1, {versionParameterName}, SYSUTCDATETIME());";
        }
    }
}
