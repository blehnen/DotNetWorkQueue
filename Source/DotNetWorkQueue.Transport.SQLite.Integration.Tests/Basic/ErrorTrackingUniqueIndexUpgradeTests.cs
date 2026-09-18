using System;
using System.Collections.Generic;
using System.Data.SQLite;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Basic
{
    /// <summary>
    /// Schema version 1: the unique index on the error tracking table's (QueueID, ExceptionType).
    /// </summary>
    /// <remarks>
    /// The shipped version, unlike SchemaUpgradeTests, which uses a version of its own to test the
    /// framework around it. SQLite because it needs no server; the SQL is shared, and the two server
    /// transports cover their own dialects separately (GitHub #308).
    /// </remarks>
    [TestClass]
    [Retry(1)]
    public class ErrorTrackingUniqueIndexUpgradeTests
    {
        [TestMethod]
        public void AQueueFromBeforeTheIndex_GetsItAndReadsAsCurrent()
        {
            using var harness = new Harness();
            harness.MakeLookLikeAPreIndexQueue();

            Assert.IsFalse(harness.UniqueIndexExists(), "setup did not remove the index");
            Assert.AreEqual(0, harness.Updater.CurrentSchemaVersion);

            var result = harness.Updater.UpgradeSchema();

            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, result.Status, result.ErrorMessage);
            Assert.AreEqual(1, result.EndingVersion);
            Assert.IsTrue(harness.UniqueIndexExists(), "the upgrade did not create the index");
            Assert.AreEqual(1, harness.Updater.CurrentSchemaVersion);
        }

        [TestMethod]
        public void DuplicateRows_CollapseToOneHoldingTheHighestCount()
        {
            using var harness = new Harness();
            harness.MakeLookLikeAPreIndexQueue();

            //what the check-then-write path leaves behind when two failures of one message race: two
            //rows for the same pair, each holding an absolute total rather than a share of one
            harness.InsertErrorRow(queueId: 1, exceptionType: "System.TimeoutException", retryCount: 3);
            harness.InsertErrorRow(queueId: 1, exceptionType: "System.TimeoutException", retryCount: 4);

            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, harness.Updater.UpgradeSchema().Status);

            var rows = harness.ReadErrorRows();
            Assert.ContainsSingle(rows);
            Assert.AreEqual(4, rows[0].RetryCount,
                "the surviving row must carry the largest count, not the sum: the column is a total " +
                "of failures so far, and adding them would invent failures that never happened");
        }

        [TestMethod]
        public void RowsThatAreNotDuplicates_AreLeftAlone()
        {
            using var harness = new Harness();
            harness.MakeLookLikeAPreIndexQueue();

            harness.InsertErrorRow(queueId: 1, exceptionType: "System.TimeoutException", retryCount: 3);
            harness.InsertErrorRow(queueId: 1, exceptionType: "System.TimeoutException", retryCount: 4);
            //same message, different exception type
            harness.InsertErrorRow(queueId: 1, exceptionType: "System.InvalidOperationException", retryCount: 2);
            //different message, same exception type
            harness.InsertErrorRow(queueId: 2, exceptionType: "System.TimeoutException", retryCount: 7);

            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, harness.Updater.UpgradeSchema().Status);

            var rows = harness.ReadErrorRows();
            Assert.AreEqual(3, rows.Count, "de-duplication removed rows that were not duplicates");
            Assert.AreEqual(4, harness.RetryCountFor(rows, 1, "System.TimeoutException"));
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
            Assert.AreEqual(0, harness.Updater.CurrentSchemaVersion);

            var result = harness.Updater.UpgradeSchema();

            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, result.Status, result.ErrorMessage);
            Assert.AreEqual(1, harness.Updater.CurrentSchemaVersion);
            Assert.IsTrue(harness.UniqueIndexExists());
        }

        [TestMethod]
        public void AfterTheUpgrade_ASecondRowForTheSamePairIsRefused()
        {
            //the point of the index: without it the second insert succeeds and the retry count reads
            //low, so the message gets more attempts than it was configured for
            using var harness = new Harness();
            harness.MakeLookLikeAPreIndexQueue();
            harness.InsertErrorRow(queueId: 1, exceptionType: "System.TimeoutException", retryCount: 1);

            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, harness.Updater.UpgradeSchema().Status);

            Assert.ThrowsExactly<SQLiteException>(
                () => harness.InsertErrorRow(queueId: 1, exceptionType: "System.TimeoutException", retryCount: 2));
        }

        [TestMethod]
        public void UpgradingTwice_IsRefusedTheSecondTimeRatherThanRepeated()
        {
            using var harness = new Harness();
            harness.MakeLookLikeAPreIndexQueue();

            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, harness.Updater.UpgradeSchema().Status);
            var second = harness.Updater.UpgradeSchema();

            Assert.AreEqual(SchemaUpgradeStatus.AlreadyCurrent, second.Status, second.ErrorMessage);
            Assert.IsTrue(second.Success);
        }

        [TestMethod]
        public void AProducerAndConsumer_AreRefusedUntilAPreIndexQueueIsUpgraded()
        {
            //the consequence a user meets first: an existing queue stops working until it is upgraded.
            //SchemaUpgradeTests covers the same guard against a version of its own; this is the shipped
            //version actually putting a real queue behind it.
            using var harness = new Harness();
            harness.MakeLookLikeAPreIndexQueue();

            var queueConnection = new QueueConnection(harness.QueueName, harness.ConnectionString);
            using var container = new QueueContainer<SqLiteMessageQueueInit>();

            var failure = Assert.ThrowsExactly<QueueSchemaOutOfDateException>(
                () => container.CreateConsumer(queueConnection));
            Assert.ThrowsExactly<QueueSchemaOutOfDateException>(
                () => container.CreateProducer<FakeMessage>(queueConnection));

            Assert.Contains(harness.QueueName, failure.Message);
            Assert.AreEqual(0, failure.CurrentVersion);
            Assert.AreEqual(1, failure.TargetVersion);

            Assert.IsTrue(harness.Updater.UpgradeSchema().Success);

            using var consumer = container.CreateConsumer(queueConnection);
            using var producer = container.CreateProducer<FakeMessage>(queueConnection);
            Assert.IsNotNull(consumer);
            Assert.IsNotNull(producer);
        }

        /// <summary>
        /// A queue on a temporary SQLite file, with the shipped updater rather than a test one.
        /// </summary>
        private sealed class Harness : IDisposable
        {
            private readonly IntegrationConnectionInfo _connectionInfo;
            private readonly QueueCreationContainer<SqLiteMessageQueueInit> _creationContainer;
            private readonly SqLiteMessageQueueCreation _creation;
            private readonly QueueContainer<SqLiteMessageQueueInit> _container;
            private readonly IContainer _admin;

            public Harness()
            {
                _connectionInfo = new IntegrationConnectionInfo(false);
                QueueName = GenerateQueueName.Create();
                ConnectionString = _connectionInfo.ConnectionString;
                var queueConnection = new QueueConnection(QueueName, ConnectionString);

                _creationContainer = new QueueCreationContainer<SqLiteMessageQueueInit>();
                _creation = _creationContainer.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection);
                var created = _creation.CreateQueue();
                Assert.IsTrue(created.Success, created.ErrorMessage);

                _container = new QueueContainer<SqLiteMessageQueueInit>();
                _admin = _container.CreateAdminContainer(queueConnection);
                Updater = _admin.GetInstance<IQueueSchemaVersion>();
            }

            public string QueueName { get; }
            public string ConnectionString { get; }
            public IQueueSchemaVersion Updater { get; }

            private string ErrorTrackingTable => QueueName + "ErrorTracking";

            /// <summary>
            /// Leaves the queue as one created before #299: no unique index, no version table.
            /// </summary>
            public void MakeLookLikeAPreIndexQueue()
            {
                DropSchemaVersionTable();
                var name = UniqueIndexName();
                Assert.IsNotNull(name, "a newly created queue should have carried the index");
                Execute($"drop index if exists {name}");
            }

            public void DropSchemaVersionTable()
            {
                Execute($"drop table if exists {QueueName}SchemaVersion");
            }

            public bool UniqueIndexExists() => UniqueIndexName() != null;

            /// <summary>
            /// The name of the unique index over (QueueID, ExceptionType), found by its shape.
            /// </summary>
            /// <remarks>
            /// By shape rather than by name for the same reason the library looks for it that way:
            /// SQLite's script writer appends the table to an index name, so matching on the name
            /// would be asserting the writer's convention rather than that the index is there.
            /// </remarks>
            private string UniqueIndexName()
            {
                using var connection = new SQLiteConnection(ConnectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = $@"SELECT il.name FROM pragma_index_list('{ErrorTrackingTable}') il
                      WHERE il.""unique"" = 1 AND il.""partial"" = 0
                      AND (SELECT COUNT(*) FROM pragma_index_info(il.name)) = 2
                      AND EXISTS (SELECT 1 FROM pragma_index_info(il.name) ii WHERE lower(ii.name) = 'queueid')
                      AND EXISTS (SELECT 1 FROM pragma_index_info(il.name) ii WHERE lower(ii.name) = 'exceptiontype')";
                return command.ExecuteScalar() as string;
            }

            public void InsertErrorRow(long queueId, string exceptionType, int retryCount)
            {
                using var connection = new SQLiteConnection(ConnectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    $"insert into {ErrorTrackingTable} (QueueID, ExceptionType, RetryCount) " +
                    "values (@QueueID, @ExceptionType, @RetryCount)";
                command.Parameters.AddWithValue("@QueueID", queueId);
                command.Parameters.AddWithValue("@ExceptionType", exceptionType);
                command.Parameters.AddWithValue("@RetryCount", retryCount);
                command.ExecuteNonQuery();
            }

            public List<(long QueueId, string ExceptionType, int RetryCount)> ReadErrorRows()
            {
                var rows = new List<(long, string, int)>();
                using var connection = new SQLiteConnection(ConnectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    $"select QueueID, ExceptionType, RetryCount from {ErrorTrackingTable} order by QueueID, ExceptionType";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    rows.Add((reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2)));
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

            private void Execute(string sql)
            {
                using var connection = new SQLiteConnection(ConnectionString);
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
                    _connectionInfo?.Dispose();
                }
            }
        }
    }
}
