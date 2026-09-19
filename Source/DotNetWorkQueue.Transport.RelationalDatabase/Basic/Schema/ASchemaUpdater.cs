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
        private readonly ISchemaTableProbe _tableProbe;
        private readonly ISchemaUpgradeLock _upgradeLock;
        private readonly object _versionsLoaded = new object();

        //volatile: read outside the lock on the fast path of a double-checked load, so without it the
        //dictionary could be seen before the writes that filled it
        private volatile bool _loaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="ASchemaUpdater"/> class.
        /// </summary>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableNameHelper">The table names for this queue.</param>
        /// <param name="tableProbe">Answers whether a table exists.</param>
        /// <param name="upgradeLock">Stops two processes upgrading at once.</param>
        /// <param name="logger">The logger.</param>
        protected ASchemaUpdater(IDbConnectionFactory connectionFactory,
            ITransactionFactory transactionFactory,
            IConnectionInformation connectionInformation,
            ITableNameHelper tableNameHelper,
            ISchemaTableProbe tableProbe,
            ISchemaUpgradeLock upgradeLock,
            ILogger logger)
        {
            Guard.NotNull(connectionFactory);
            Guard.NotNull(transactionFactory);
            Guard.NotNull(connectionInformation);
            Guard.NotNull(tableNameHelper);
            Guard.NotNull(tableProbe);
            Guard.NotNull(upgradeLock);

            _connectionFactory = connectionFactory;
            _transactionFactory = transactionFactory;
            _connectionInformation = connectionInformation;
            _tableProbe = tableProbe;
            _upgradeLock = upgradeLock;

            TableNameHelper = tableNameHelper;
            Logger = logger;
            Versions = new SortedDictionary<long, ISchemaVersion>();
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
        /// <remarks>
        /// Numbered from one and contiguous. A gap is treated as a fault rather than skipped, because
        /// a queue sitting in the gap would be told it had reached the target without the missing
        /// version ever running.
        /// </remarks>
        protected SortedDictionary<long, ISchemaVersion> Versions { get; }

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
                if (!_tableProbe.Exists(_connectionInformation.ConnectionString, TableNameHelper.QueueName))
                    return TargetSchemaVersion;

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
            var queueName = TableNameHelper.QueueName;

            if (!_tableProbe.Exists(_connectionInformation.ConnectionString, queueName))
            {
                return new SchemaUpgradeResult(SchemaUpgradeStatus.QueueDoesNotExist, 0, 0,
                    $"The queue {queueName} does not exist.");
            }

            var startingVersion = 0L;
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

                    using (var transaction = BeginUpgradeTransaction(connection))
                    {
                        if (!_upgradeLock.TryAcquire(connection, transaction, queueName, LockTimeout))
                        {
                            transaction.Rollback();
                            return AfterLosingTheLock(startingVersion, target);
                        }

                        //re-read under the lock: the process we queued behind may have done the work
                        var current = ReadVersion(connection, transaction);
                        if (current >= target)
                        {
                            transaction.Rollback();
                            return new SchemaUpgradeResult(SchemaUpgradeStatus.AlreadyCurrent, current, current);
                        }

                        var missing = FirstMissingVersionAfter(current);
                        if (missing.HasValue)
                        {
                            transaction.Rollback();
                            return new SchemaUpgradeResult(SchemaUpgradeStatus.Failed, current, current,
                                $"Version {missing.Value} is missing, so the queue cannot be taken from " +
                                $"{current} to {target} without skipping it.");
                        }

                        Logger?.LogInformation(
                            "Upgrading the schema for {Queue} from version {Current} to {Target}",
                            queueName, current, target);

                        CreateVersionTableIfMissing(connection, transaction);

                        foreach (var version in Versions.Where(x => x.Key > current).OrderBy(x => x.Key))
                        {
                            Apply(version.Key, version.Value, queueName, connection, transaction);
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
                            queueName, target);
                        return new SchemaUpgradeResult(SchemaUpgradeStatus.Upgraded, startingVersion, target);
                    }
                }
            }
            catch (Exception error)
            {
                //the transaction is gone, so the schema is whatever it was before this ran - report that
                //version rather than zero, which would read as "this queue has never been upgraded"
                Logger?.LogError(error, "Failed to upgrade the schema for {Queue}", queueName);
                return new SchemaUpgradeResult(SchemaUpgradeStatus.Failed, startingVersion, startingVersion,
                    error.Message);
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
        /// Starts the transaction the upgrade runs in.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <remarks>
        /// Overridable because one transport's exclusion depends on how the transaction begins rather
        /// than on a lock it takes. Everything else wants the connection's usual behaviour.
        /// </remarks>
        protected virtual DbTransaction BeginUpgradeTransaction(DbConnection connection) =>
            _transactionFactory.Create(connection).BeginTransaction();

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

        /// <summary>
        /// The first version between <paramref name="current"/> and the target that is not registered.
        /// </summary>
        /// <remarks>
        /// Applying "everything above the current version" silently tolerates a gap: with versions 1
        /// and 3 registered, a queue at 1 would run 3, be stamped 3 and be reported as fully upgraded,
        /// having never run 2. Whatever 2 was supposed to do is then missing forever, and nothing says
        /// so. Refusing before touching anything is the only honest answer.
        /// </remarks>
        private long? FirstMissingVersionAfter(long current)
        {
            var target = TargetSchemaVersion;
            for (var expected = current + 1; expected <= target; expected++)
            {
                if (!Versions.ContainsKey(expected))
                    return expected;
            }

            return null;
        }

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

        /// <summary>
        /// What to report when another process holds the lock.
        /// </summary>
        private SchemaUpgradeResult AfterLosingTheLock(long startingVersion, long target)
        {
            using (var connection = _connectionFactory.Create())
            {
                connection.Open();
                var current = ReadVersion(connection, null);
                if (current >= target)
                    return new SchemaUpgradeResult(SchemaUpgradeStatus.AlreadyCurrent, startingVersion, current);

                //the holder may still be mid-upgrade rather than failed, so this is its own status:
                //reporting Failed would invite a caller to treat a running upgrade as a dead one
                return new SchemaUpgradeResult(SchemaUpgradeStatus.UpgradeInProgressElsewhere,
                    startingVersion, current,
                    "Another process holds the upgrade lock and has not finished. Nothing was changed " +
                    "here; run the upgrade again once it has.");
            }
        }

        private long ReadVersion(DbConnection connection, DbTransaction transaction)
        {
            //no table means the queue predates versioning, which reads as zero rather than as an error
            var exists = transaction == null
                ? _tableProbe.Exists(_connectionInformation.ConnectionString, TableNameHelper.SchemaVersionName)
                : _tableProbe.Exists(connection, transaction, TableNameHelper.SchemaVersionName);

            if (!exists)
                return 0;

            using (var command = connection.CreateCommand())
            {
                if (transaction != null)
                    command.Transaction = transaction;

                //A table name is an identifier, and no provider here binds an identifier as a
                //parameter, so this cannot be the parameterised query S2077 asks for.
                //
                //What makes it safe is that the name is not free text. It comes from
                //ITableNameHelper, built from the queue name, and the three transports that reach
                //this code each validate that name in their IConnectionInformation constructor
                //before any of it runs:
                //
                //    SQL Server   ^[a-zA-Z0-9_.]+$
                //    PostgreSQL   ^[a-zA-Z0-9_]+$
                //    SQLite       ^[a-zA-Z0-9_.]+$
                //
                //The three are not the same - PostgreSQL rejects the dot the other two allow, since
                //a dotted name cannot be created there at all (#375) - but none of them admits a
                //quote, semicolon, whitespace or comment marker, so there is nothing to break out
                //of. Redis validates a different set again and Memory validates nothing, but
                //neither has a schema to upgrade, so neither arrives here.
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

        private void Apply(long versionNumber, ISchemaVersion version, string queueName,
            DbConnection connection, DbTransaction transaction)
        {
            var script = version.Script(TableNameHelper, connection, transaction);
            if (string.IsNullOrWhiteSpace(script))
            {
                throw new InvalidOperationException(
                    $"Schema version {versionNumber} ({version.GetType().Name}) produced an empty script.");
            }

            Logger?.LogInformation("Applying schema version {Version} to {Queue}", versionNumber, queueName);

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = script;
                command.ExecuteNonQuery();
            }
        }
    }
}
