using System;
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Npgsql;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Basic
{
    /// <summary>
    /// Schema version 1 on PostgreSQL: the unique index on the error tracking table's
    /// (QueueID, ExceptionType), and the collapse of the duplicate rows that its absence allowed.
    /// </summary>
    /// <remarks>
    /// The SQLite suite covers the same version, but the statements are not the same statements: the
    /// index DDL is written per transport, and the de-duplication correlates a subquery against the
    /// table being updated, which every dialect spells its own way. Running it here is the only thing
    /// that shows it parses and means what it should on PostgreSQL (GitHub #308).
    /// </remarks>
    [TestClass]
    [Retry(1)]
    public class ErrorTrackingUniqueIndexUpgradeTests
    {
        [TestMethod]
        public void AQueueFromBeforeTheIndex_GetsItAndCollapsesTheDuplicatesItAllowed()
        {
            using var harness = new Harness();
            harness.MakeLookLikeAPreIndexQueue();

            Assert.IsFalse(harness.UniqueIndexExists(), "setup did not remove the index");
            Assert.AreEqual(0, harness.Updater.CurrentSchemaVersion);

            //two rows for one pair is what the check-then-write path leaves behind under a race. Each
            //count is an absolute total, so the pair collapses to the larger rather than to their sum.
            harness.InsertErrorRow(1, "System.TimeoutException", 3);
            harness.InsertErrorRow(1, "System.TimeoutException", 4);
            //and rows that are not duplicates have to survive untouched
            harness.InsertErrorRow(1, "System.InvalidOperationException", 2);
            harness.InsertErrorRow(2, "System.TimeoutException", 7);

            var result = harness.Updater.UpgradeSchema();

            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, result.Status, result.ErrorMessage);
            Assert.AreEqual(1, result.EndingVersion);
            Assert.IsTrue(harness.UniqueIndexExists(), "the upgrade did not create the index");
            Assert.AreEqual(1, harness.Updater.CurrentSchemaVersion);

            var rows = harness.ReadErrorRows();
            Assert.AreEqual(3, rows.Count, "de-duplication removed rows that were not duplicates");
            Assert.AreEqual(4, harness.RetryCountFor(rows, 1, "System.TimeoutException"),
                "the surviving row must carry the largest count rather than the sum");
            Assert.AreEqual(2, harness.RetryCountFor(rows, 1, "System.InvalidOperationException"));
            Assert.AreEqual(7, harness.RetryCountFor(rows, 2, "System.TimeoutException"));
        }

        [TestMethod]
        public void AQueueThatAlreadyHasTheIndex_UpgradesInsteadOfFailingOnIt()
        {
            //a queue created after #299 but before schema versioning: the index is there, the version
            //table is not. Creating the index again would fail and take the whole upgrade with it.
            using var harness = new Harness();
            harness.DropSchemaVersionTable();

            Assert.IsTrue(harness.UniqueIndexExists(), "expected a newly created queue to carry the index");

            var result = harness.Updater.UpgradeSchema();

            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, result.Status, result.ErrorMessage);
            Assert.AreEqual(1, harness.Updater.CurrentSchemaVersion);
            Assert.IsTrue(harness.UniqueIndexExists());
        }

        [TestMethod]
        public void AfterTheUpgrade_ASecondRowForTheSamePairIsRefused()
        {
            using var harness = new Harness();
            harness.MakeLookLikeAPreIndexQueue();
            harness.InsertErrorRow(1, "System.TimeoutException", 1);

            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, harness.Updater.UpgradeSchema().Status);

            //without the index this insert succeeds, the retry count reads low, and the message gets
            //more attempts than it was configured for
            Assert.ThrowsExactly<PostgresException>(
                () => harness.InsertErrorRow(1, "System.TimeoutException", 2));
        }

        /// <summary>
        /// A queue on the PostgreSQL test server, with the shipped updater.
        /// </summary>
        private sealed class Harness : IDisposable
        {
            private readonly QueueCreationContainer<PostgreSqlMessageQueueInit> _creationContainer;
            private readonly PostgreSqlMessageQueueCreation _creation;
            private readonly QueueContainer<PostgreSqlMessageQueueInit> _container;
            private readonly IContainer _admin;
            private readonly string _errorTrackingTable;

            public Harness()
            {
                QueueName = GenerateQueueName.Create();
                var queueConnection = new QueueConnection(QueueName, ConnectionInfo.ConnectionString);

                _creationContainer = new QueueCreationContainer<PostgreSqlMessageQueueInit>();
                _creation = _creationContainer.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
                var created = _creation.CreateQueue();
                Assert.IsTrue(created.Success, created.ErrorMessage);

                _container = new QueueContainer<PostgreSqlMessageQueueInit>();
                _admin = _container.CreateAdminContainer(queueConnection);
                Updater = _admin.GetInstance<IQueueSchemaVersion>();

                //from the helper rather than built here, so the name carries whatever schema the
                //connection is configured for
                _errorTrackingTable = _admin.GetInstance<ITableNameHelper>().ErrorTrackingName;
            }

            public string QueueName { get; }
            public IQueueSchemaVersion Updater { get; }

            /// <summary>
            /// Leaves the queue as one created before #299: no unique index, no version table.
            /// </summary>
            public void MakeLookLikeAPreIndexQueue()
            {
                DropSchemaVersionTable();
                var name = UniqueIndexName();
                Assert.IsNotNull(name, "a newly created queue should have carried the index");
                Execute($"drop index {name}");
            }

            public void DropSchemaVersionTable()
            {
                Execute($"drop table if exists {QueueName}SchemaVersion");
            }

            public bool UniqueIndexExists() => UniqueIndexName() != null;

            /// <summary>
            /// The unique index over (QueueID, ExceptionType), found by its shape rather than its name
            /// - which is how the library looks for it, and the only way to tell an index that carries
            /// the constraint from one that merely includes the columns.
            /// </summary>
            private string UniqueIndexName()
            {
                using var connection = new NpgsqlConnection(ConnectionInfo.ConnectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = @"SELECT ic.relname FROM pg_index ix
                      JOIN pg_class ic ON ic.oid = ix.indexrelid
                      WHERE ix.indrelid = to_regclass(@Table)
                      AND ix.indisunique AND ix.indpred IS NULL
                      AND ix.indnkeyatts = 2
                      AND EXISTS (SELECT 1 FROM pg_attribute a
                                  WHERE a.attrelid = ix.indrelid AND a.attnum IN (ix.indkey[0], ix.indkey[1]) AND lower(a.attname) = 'queueid')
                      AND EXISTS (SELECT 1 FROM pg_attribute a
                                  WHERE a.attrelid = ix.indrelid AND a.attnum IN (ix.indkey[0], ix.indkey[1]) AND lower(a.attname) = 'exceptiontype')";
                var table = command.CreateParameter();
                table.ParameterName = "@Table";
                table.Value = _errorTrackingTable;
                command.Parameters.Add(table);
                var result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? null : Convert.ToString(result);
            }

            public void InsertErrorRow(long queueId, string exceptionType, int retryCount)
            {
                using var connection = new NpgsqlConnection(ConnectionInfo.ConnectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    $"insert into {_errorTrackingTable} (QueueID, ExceptionType, RetryCount) " +
                    "values (@QueueID, @ExceptionType, @RetryCount)";
                Add(command, "@QueueID", queueId);
                Add(command, "@ExceptionType", exceptionType);
                Add(command, "@RetryCount", retryCount);
                command.ExecuteNonQuery();
            }

            public List<(long QueueId, string ExceptionType, int RetryCount)> ReadErrorRows()
            {
                var rows = new List<(long, string, int)>();
                using var connection = new NpgsqlConnection(ConnectionInfo.ConnectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    $"select QueueID, ExceptionType, RetryCount from {_errorTrackingTable} " +
                    "order by QueueID, ExceptionType";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    rows.Add((Convert.ToInt64(reader.GetValue(0)), reader.GetString(1),
                        Convert.ToInt32(reader.GetValue(2))));
                }

                return rows;
            }

            public int RetryCountFor(List<(long QueueId, string ExceptionType, int RetryCount)> rows,
                long queueId, string exceptionType)
            {
                foreach (var row in rows)
                {
                    if (row.QueueId == queueId && row.ExceptionType == exceptionType)
                        return row.RetryCount;
                }

                Assert.Fail($"no row for ({queueId}, {exceptionType})");
                return 0;
            }

            private static void Add(System.Data.Common.DbCommand command, string name, object value)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value;
                command.Parameters.Add(parameter);
            }

            private void Execute(string sql)
            {
                using var connection = new NpgsqlConnection(ConnectionInfo.ConnectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }

            public void Dispose()
            {
                _admin?.Dispose();
                _container?.Dispose();
                try
                {
                    _creation.RemoveQueue();
                }
                finally
                {
                    _creation?.Dispose();
                    _creationContainer?.Dispose();
                }
            }
        }
    }
}
