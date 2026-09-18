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
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema
{
    /// <summary>
    /// Brings a queue's schema up to the version this library expects.
    /// </summary>
    /// <remarks>
    /// The whole upgrade is one transaction holding one lock. Either the queue ends at the target
    /// version or it is untouched; there is no state in between for someone to find later and have to
    /// reason about.
    ///
    /// Producers and consumers refuse to start against an out-of-date queue, so an upgrade runs with
    /// nothing else attached to it. That is what makes a plain locking CREATE INDEX acceptable here and
    /// removes the need for PostgreSQL's CONCURRENTLY, which cannot run inside a transaction anyway.
    /// </remarks>
    public abstract class ASchemaUpdater : IQueueSchemaVersion, ISchemaVersionStamp
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ITransactionFactory _transactionFactory;
        private readonly IConnectionInformation _connectionInformation;
        private readonly IQueryHandler<GetTableExistsQuery, bool> _tableExists;
        private readonly IQueryHandler<GetTableExistsTransactionQuery, bool> _tableExistsInTransaction;
        private readonly ISchemaUpgradeLock _upgradeLock;
        private readonly object _versionsLoaded = new object();
        private bool _loaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="ASchemaUpdater"/> class.
        /// </summary>
        protected ASchemaUpdater(IDbConnectionFactory connectionFactory,
            ITransactionFactory transactionFactory,
            IConnectionInformation connectionInformation,
            ITableNameHelper tableNameHelper,
            IQueryHandler<GetTableExistsQuery, bool> tableExists,
            IQueryHandler<GetTableExistsTransactionQuery, bool> tableExistsInTransaction,
            ISchemaUpgradeLock upgradeLock,
            ILogger logger)
        {
            Guard.NotNull(connectionFactory);
            Guard.NotNull(transactionFactory);
            Guard.NotNull(connectionInformation);
            Guard.NotNull(tableNameHelper);
            Guard.NotNull(tableExists);
            Guard.NotNull(tableExistsInTransaction);
            Guard.NotNull(upgradeLock);

            _connectionFactory = connectionFactory;
            _transactionFactory = transactionFactory;
            _connectionInformation = connectionInformation;
            _tableExists = tableExists;
            _tableExistsInTransaction = tableExistsInTransaction;
            _upgradeLock = upgradeLock;

            TableNameHelper = tableNameHelper;
            Logger = logger;
            Versions = new SortedDictionary<long, ASchemaVersion>();
        }

        /// <summary>
        /// The table names for the queue being upgraded.
        /// </summary>
        protected ITableNameHelper TableNameHelper { get; }

        /// <summary>
        /// The logger. May be null.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Every version, lowest first.
        /// </summary>
        protected SortedDictionary<long, ASchemaVersion> Versions { get; }

        /// <summary>
        /// How long to wait for the upgrade lock before concluding another process has it.
        /// </summary>
        protected virtual TimeSpan LockTimeout => TimeSpan.FromSeconds(30);

        /// <inheritdoc />
        public long TargetSchemaVersion
        {
            get
            {
                EnsureVersionsLoaded();
                return Versions.Count == 0 ? 0 : Versions.Keys.Last();
            }
        }

        /// <inheritdoc />
        public long CurrentSchemaVersion
        {
            get
            {
                //a queue that is not there has no version, and saying 0 would read as "needs upgrading"
                if (!_tableExists.Handle(new GetTableExistsQuery(_connectionInformation.ConnectionString,
                        TableNameHelper.QueueName)))
                {
                    return TargetSchemaVersion;
                }

                using (var connection = _connectionFactory.Create())
                {
                    connection.Open();
                    return ReadVersion(connection, null);
                }
            }
        }

        /// <inheritdoc />
        public SchemaUpgradeResult UpgradeSchema()
        {
            EnsureVersionsLoaded();
            var target = TargetSchemaVersion;

            if (!_tableExists.Handle(new GetTableExistsQuery(_connectionInformation.ConnectionString,
                    TableNameHelper.QueueName)))
            {
                return new SchemaUpgradeResult(SchemaUpgradeStatus.QueueDoesNotExist, 0, 0,
                    $"The queue {TableNameHelper.QueueName} does not exist.");
            }

            long startingVersion;
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    connection.Open();
                    startingVersion = ReadVersion(connection, null);
                    if (startingVersion >= target)
                    {
                        return new SchemaUpgradeResult(SchemaUpgradeStatus.AlreadyCurrent,
                            startingVersion, startingVersion);
                    }

                    using (var transaction = _transactionFactory.Create(connection).BeginTransaction())
                    {

                        if (!_upgradeLock.TryAcquire(connection, transaction,
                                TableNameHelper.QueueName, LockTimeout))
                        {
                            //someone else is upgrading. Not a failure of ours - report what they left.
                            transaction.Rollback();
                            return VersionAfterAnotherProcess(startingVersion, target);
                        }

                        //re-read under the lock: the process we queued behind may have done the work
                        var current = ReadVersion(connection, transaction);
                        if (current >= target)
                        {
                            transaction.Rollback();
                            return new SchemaUpgradeResult(SchemaUpgradeStatus.AlreadyCurrent,
                                current, current);
                        }

                        Logger?.LogInformation(
                            "Upgrading the schema for {Queue} from version {Current} to {Target}",
                            TableNameHelper.QueueName, current, target);

                        CreateVersionTableIfMissing(connection, transaction);

                        foreach (var version in Versions.Where(x => x.Key > current).OrderBy(x => x.Key))
                        {
                            Apply(version.Key, version.Value, connection, transaction);
                            WriteVersion(version.Key, connection, transaction);
                        }

                        //trust the database rather than the loop that just ran
                        var applied = ReadVersion(connection, transaction);
                        if (applied != target)
                        {
                            transaction.Rollback();
                            return new SchemaUpgradeResult(SchemaUpgradeStatus.Failed, startingVersion,
                                startingVersion,
                                $"Applied every version but the schema reports {applied} rather than {target}.");
                        }

                        transaction.Commit();
                        Logger?.LogInformation("Upgraded the schema for {Queue} to version {Target}",
                            TableNameHelper.QueueName, target);
                        return new SchemaUpgradeResult(SchemaUpgradeStatus.Upgraded, startingVersion, target);
                    }
                }
            }
            catch (Exception error)
            {
                //the transaction is gone, so the schema is whatever it was before this ran
                Logger?.LogError(error, "Failed to upgrade the schema for {Queue}",
                    TableNameHelper.QueueName);
                return new SchemaUpgradeResult(SchemaUpgradeStatus.Failed, 0, 0, error.Message);
            }
        }

        /// <inheritdoc />
        public void MarkCurrent(DbConnection connection, DbTransaction transaction)
        {
            EnsureVersionsLoaded();
            var target = TargetSchemaVersion;

            //nothing to record until this transport has a version, so a new queue pays nothing
            if (target == 0)
                return;

            CreateVersionTableIfMissing(connection, transaction);
            WriteVersion(target, connection, transaction);
        }

        /// <summary>
        /// Adds every version to <see cref="Versions"/>.
        /// </summary>
        protected abstract void LoadVersions();

        /// <summary>
        /// The script creating the version table. Must tolerate the table already existing.
        /// </summary>
        /// <param name="tableName">The version table's name.</param>
        protected abstract string CreateVersionTableScript(string tableName);

        /// <summary>
        /// The script writing the version, inserting or updating the single row as needed.
        /// </summary>
        /// <param name="tableName">The version table's name.</param>
        /// <param name="versionParameterName">The parameter holding the version, including its prefix.</param>
        protected abstract string WriteVersionScript(string tableName, string versionParameterName);

        private void EnsureVersionsLoaded()
        {
            if (_loaded)
                return;

            lock (_versionsLoaded)
            {
                if (_loaded)
                    return;

                LoadVersions();
                _loaded = true;
            }
        }

        private SchemaUpgradeResult VersionAfterAnotherProcess(long startingVersion, long target)
        {
            using (var connection = _connectionFactory.Create())
            {
                connection.Open();
                var current = ReadVersion(connection, null);
                return current >= target
                    ? new SchemaUpgradeResult(SchemaUpgradeStatus.AlreadyCurrent, startingVersion, current)
                    : new SchemaUpgradeResult(SchemaUpgradeStatus.Failed, startingVersion, current,
                        "Another process holds the upgrade lock and the schema is still out of date. " +
                        "Wait for it to finish and try again.");
            }
        }

        private long ReadVersion(DbConnection connection, DbTransaction transaction)
        {
            //no table means the queue predates versioning, which reads as zero rather than as an error
            var exists = transaction == null
                ? _tableExists.Handle(new GetTableExistsQuery(_connectionInformation.ConnectionString,
                    TableNameHelper.SchemaVersionName))
                : _tableExistsInTransaction.Handle(new GetTableExistsTransactionQuery(connection, transaction,
                    TableNameHelper.SchemaVersionName));

            if (!exists)
                return 0;

            using (var command = connection.CreateCommand())
            {
                if (transaction != null)
                    command.Transaction = transaction;

                command.CommandText = $"select Version from {TableNameHelper.SchemaVersionName}";
                var result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt64(result);
            }
        }

        private void CreateVersionTableIfMissing(DbConnection connection, DbTransaction transaction)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = CreateVersionTableScript(TableNameHelper.SchemaVersionName);
                command.ExecuteNonQuery();
            }
        }

        private void WriteVersion(long version, DbConnection connection, DbTransaction transaction)
        {
            //every transport here binds with @, including Npgsql
            const string parameterName = "@Version";
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = WriteVersionScript(TableNameHelper.SchemaVersionName, parameterName);

                var parameter = command.CreateParameter();
                parameter.ParameterName = parameterName;
                parameter.Value = version;
                command.Parameters.Add(parameter);

                command.ExecuteNonQuery();
            }
        }

        private void Apply(long versionNumber, ASchemaVersion version, DbConnection connection,
            DbTransaction transaction)
        {
            var script = version.Script(TableNameHelper, connection, transaction);
            if (string.IsNullOrWhiteSpace(script))
            {
                throw new InvalidOperationException(
                    $"Schema version {versionNumber} ({version.GetType().Name}) produced an empty script.");
            }

            Logger?.LogInformation("Applying schema version {Version} to {Queue}", versionNumber,
                TableNameHelper.QueueName);

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = script;
                command.ExecuteNonQuery();
            }
        }
    }
}
