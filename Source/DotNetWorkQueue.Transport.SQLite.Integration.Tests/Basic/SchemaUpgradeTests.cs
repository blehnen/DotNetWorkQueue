using System;
using System.Data.Common;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Basic
{
    /// <summary>
    /// The schema upgrade framework, exercised through a version that exists only here.
    /// </summary>
    /// <remarks>
    /// No shipped transport declares a version yet, so without a test-only one there is nothing to
    /// upgrade and the framework would go out unproven. SQLite carries these because it needs no
    /// server: the behaviour under test is in the shared base, not in the transport (GitHub #308).
    /// </remarks>
    [TestClass]
    [Retry(1)]
    public class SchemaUpgradeTests
    {
        [TestMethod]
        public void AQueueAtVersionZero_IsUpgraded()
        {
            using var harness = new Harness();

            Assert.AreEqual(0, harness.Updater.CurrentSchemaVersion, "a new queue should read as version 0");
            Assert.AreEqual(1, harness.Updater.TargetSchemaVersion);

            var result = harness.Updater.UpgradeSchema();

            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, result.Status, result.ErrorMessage);
            Assert.AreEqual(0, result.StartingVersion);
            Assert.AreEqual(1, result.EndingVersion);
            Assert.IsTrue(result.Success);

            Assert.AreEqual(1, harness.Updater.CurrentSchemaVersion);
            Assert.IsTrue(harness.TableExists(MarkerVersion.MarkerTable(harness.QueueName)),
                "the version's script did not run");
        }

        [TestMethod]
        public void UpgradingTwice_ChangesNothingTheSecondTime()
        {
            using var harness = new Harness();
            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, harness.Updater.UpgradeSchema().Status);

            var second = harness.Updater.UpgradeSchema();

            Assert.AreEqual(SchemaUpgradeStatus.AlreadyCurrent, second.Status, second.ErrorMessage);
            Assert.IsTrue(second.Success, "already current is a success: the queue is at the version asked for");
            Assert.AreEqual(1, harness.Updater.CurrentSchemaVersion);
        }

        [TestMethod]
        public void UpgradingAQueueThatDoesNotExist_SaysSo()
        {
            //deliberately no queue created
            using var harness = new Harness(createQueue: false);

            var result = harness.Updater.UpgradeSchema();

            Assert.AreEqual(SchemaUpgradeStatus.QueueDoesNotExist, result.Status);
            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public void AnUpgradeKeepsTheDataThatWasAlreadyThere()
        {
            using var harness = new Harness();

            //the configuration row is written by CreateQueue and is untouched by the upgrade, so it
            //stands in for "data that was already there". Status and History are optional tables and
            //are not created unless those features are switched on.
            var before = harness.ScalarLong($"select count(*) from {harness.QueueName}Configuration");
            Assert.AreEqual(1L, before, "expected CreateQueue to have written a configuration row");

            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, harness.Updater.UpgradeSchema().Status);

            Assert.AreEqual(before, harness.ScalarLong(
                    $"select count(*) from {harness.QueueName}Configuration"),
                "the upgrade lost data it had no business touching");
            Assert.AreEqual(1L, harness.ScalarLong(
                    $"select count(*) from {harness.QueueName}Configuration where length(Configuration) > 0"),
                "the configuration row survived but its contents did not");
        }

        [TestMethod]
        public void TheVersionIsReadBackFromTheDatabase_NotAssumed()
        {
            using var harness = new Harness();
            harness.Updater.UpgradeSchema();

            //read it without going through the updater, so a bug in its own reader cannot hide one here
            Assert.AreEqual(1L, harness.ScalarLong($"select Version from {harness.QueueName}SchemaVersion"));
        }

        [TestMethod]
        public void AProducerAndConsumerAreRefused_UntilTheQueueIsUpgraded()
        {
            using var connectionInfo = new IntegrationConnectionInfo(false);
            var queueName = GenerateQueueName.Create();
            var queueConnection = new QueueConnection(queueName, connectionInfo.ConnectionString);

            using var creationContainer = new QueueCreationContainer<SqLiteMessageQueueInit>();
            var creation = creationContainer.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection);
            try
            {
                Assert.IsTrue(creation.CreateQueue().Success);

                using var container = new QueueContainer<SqLiteMessageQueueInit>(
                    c => c.Register<IQueueSchemaVersion, TestSchemaUpdater>(LifeStyles.Singleton));

                var consumerFailure = Assert.ThrowsExactly<QueueSchemaOutOfDateException>(
                    () => container.CreateConsumer(queueConnection));
                Assert.ThrowsExactly<QueueSchemaOutOfDateException>(
                    () => container.CreateProducer<FakeMessage>(queueConnection));

                //the message has to say which queue and how far behind, or it names no action
                Assert.Contains(queueName, consumerFailure.Message);
                Assert.AreEqual(0, consumerFailure.CurrentVersion);
                Assert.AreEqual(1, consumerFailure.TargetVersion);

                //and the refusal has to stop once the queue is brought forward
                using var admin = container.CreateAdminContainer(queueConnection);
                Assert.IsTrue(admin.GetInstance<IQueueSchemaVersion>().UpgradeSchema().Success);

                using var consumer = container.CreateConsumer(queueConnection);
                using var producer = container.CreateProducer<FakeMessage>(queueConnection);
                Assert.IsNotNull(consumer);
                Assert.IsNotNull(producer);
            }
            finally
            {
                creation.RemoveQueue();
                creation.Dispose();
            }
        }

        /// <summary>
        /// Creates a queue, and an updater carrying one version, against a temporary SQLite file.
        /// </summary>
        private sealed class Harness : IDisposable
        {
            private readonly IntegrationConnectionInfo _connectionInfo;
            private readonly QueueCreationContainer<SqLiteMessageQueueInit> _creationContainer;
            private readonly SqLiteMessageQueueCreation _creation;
            private readonly QueueContainer<SqLiteMessageQueueInit> _container;
            private readonly IContainer _admin;

            public Harness(bool createQueue = true)
            {
                _connectionInfo = new IntegrationConnectionInfo(false);
                QueueName = GenerateQueueName.Create();
                var queueConnection = new QueueConnection(QueueName, _connectionInfo.ConnectionString);
                ConnectionString = _connectionInfo.ConnectionString;

                _creationContainer = new QueueCreationContainer<SqLiteMessageQueueInit>();
                _creation = _creationContainer.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection);
                if (createQueue)
                {
                    var created = _creation.CreateQueue();
                    Assert.IsTrue(created.Success, created.ErrorMessage);
                }

                //substitute an updater that has a version, since no shipped one does yet
                _container = new QueueContainer<SqLiteMessageQueueInit>(
                    c => c.Register<IQueueSchemaVersion, TestSchemaUpdater>(LifeStyles.Singleton));
                _admin = _container.CreateAdminContainer(queueConnection);
                Updater = _admin.GetInstance<IQueueSchemaVersion>();
            }

            public string QueueName { get; }
            public string ConnectionString { get; }
            public IQueueSchemaVersion Updater { get; }

            public long ScalarLong(string sql)
            {
                using var connection = new System.Data.SQLite.SQLiteConnection(ConnectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                var result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt64(result);
            }

            public bool TableExists(string tableName) =>
                ScalarLong($"select count(*) from sqlite_master where type='table' and name='{tableName}'") == 1;

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

        /// <summary>
        /// A SQLite updater carrying exactly one version, for these tests only.
        /// </summary>
        private class TestSchemaUpdater : SqLiteSchemaUpdater
        {
            public TestSchemaUpdater(IDbConnectionFactory connectionFactory,
                ITransactionFactory transactionFactory,
                IConnectionInformation connectionInformation,
                ITableNameHelper tableNameHelper,
                IQueryHandler<GetTableExistsQuery, bool> tableExists,
                IQueryHandler<GetTableExistsTransactionQuery, bool> tableExistsInTransaction,
                ISchemaUpgradeLock upgradeLock,
                ILogger logger)
                : base(connectionFactory, transactionFactory, connectionInformation, tableNameHelper,
                    tableExists, tableExistsInTransaction, upgradeLock, logger)
            {
            }

            protected override void LoadVersions()
            {
                Versions.Add(1, new MarkerVersion());
            }
        }

        /// <summary>
        /// A version that creates one table, so "did the script run" is a question with an answer.
        /// </summary>
        private class MarkerVersion : ASchemaVersion
        {
            public static string MarkerTable(string queueName) => string.Concat(queueName, "UpgradeMarker");

            public override string Script(ITableNameHelper tableNames, DbConnection connection,
                DbTransaction transaction)
            {
                return $"create table {MarkerTable(tableNames.QueueName)} (Id integer not null primary key);";
            }
        }
    }
}
