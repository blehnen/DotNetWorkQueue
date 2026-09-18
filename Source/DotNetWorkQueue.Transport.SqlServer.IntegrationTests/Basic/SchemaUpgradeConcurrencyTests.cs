using System;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.Basic.Schema;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Basic
{
    /// <summary>
    /// Two processes upgrading the same queue at once.
    /// </summary>
    /// <remarks>
    /// Here rather than in the SQLite suite because this is the test of the lock, and SQLite has no
    /// advisory lock to take - its updater returns true and relies on the database allowing a single
    /// writer. SQL Server's sp_getapplock is a real lock and is the part carrying risk,
    /// and an owner of Transaction means it releases however the transaction ends, which is the
    /// property that stops a process dying mid-upgrade from holding it forever (GitHub #308).
    /// </remarks>
    [TestClass]
    [Retry(1)]
    public class SchemaUpgradeConcurrencyTests
    {
        [TestMethod]
        public void TwoUpgradesAtOnce_OneAppliesAndTheOtherFindsItDone()
        {
            var queueName = GenerateQueueName.Create();
            var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);

            using var creationContainer = new QueueCreationContainer<SqlServerMessageQueueInit>();
            var creation = creationContainer.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection);
            try
            {
                Assert.IsTrue(creation.CreateQueue().Success);

                //CreateQueue stamps the queue at the shipped target version, so without this both
                //upgrades would find nothing to do and the race under test would not happen
                DropSchemaVersionTable(queueName);

                //two containers, so two updaters that share nothing but the database
                using var containerOne = NewContainer();
                using var containerTwo = NewContainer();
                using var adminOne = containerOne.CreateAdminContainer(queueConnection);
                using var adminTwo = containerTwo.CreateAdminContainer(queueConnection);

                var one = adminOne.GetInstance<IQueueSchemaVersion>();
                var two = adminTwo.GetInstance<IQueueSchemaVersion>();

                var results = Task.WhenAll(
                    Task.Run(() => one.UpgradeSchema()),
                    Task.Run(() => two.UpgradeSchema())).GetAwaiter().GetResult();

                foreach (var result in results)
                {
                    Assert.IsTrue(result.Success,
                        $"one of the two upgrades failed outright: {result.Status} {result.ErrorMessage}");
                }

                //the lock exists so the work happens once. Both reporting Upgraded would mean both
                //applied the scripts, which for DDL is the failure this is guarding against.
                Assert.ContainsSingle(results.Where(x => x.Status == SchemaUpgradeStatus.Upgraded),
                    "the version scripts were applied more than once");
                Assert.ContainsSingle(results.Where(x => x.Status == SchemaUpgradeStatus.AlreadyCurrent),
                    "the process that lost the race should have found the work already done");

                Assert.AreEqual(1, one.CurrentSchemaVersion);
            }
            finally
            {
                creation.RemoveQueue();
                creation.Dispose();
            }
        }

        /// <summary>
        /// Leaves the queue looking like one created before schema versioning existed.
        /// </summary>
        private static void DropSchemaVersionTable(string queueName)
        {
            using var connection = new SqlConnection(ConnectionInfo.ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"drop table if exists {queueName}SchemaVersion";
            command.ExecuteNonQuery();
        }

        private static QueueContainer<SqlServerMessageQueueInit> NewContainer() =>
            new QueueContainer<SqlServerMessageQueueInit>(
                c => c.Register<IQueueSchemaVersion, TestSchemaUpdater>(LifeStyles.Singleton));

        /// <summary>
        /// A PostgreSQL updater carrying exactly one version, for this test only.
        /// </summary>
        private class TestSchemaUpdater : SqlServerSchemaUpdater
        {
            public TestSchemaUpdater(IDbConnectionFactory connectionFactory,
                ITransactionFactory transactionFactory,
                IConnectionInformation connectionInformation,
                ITableNameHelper tableNameHelper,
                ISchemaTableProbe tableProbe,
                ISchemaUpgradeLock upgradeLock,
                CommandStringCache commandCache,
                ILogger logger)
                : base(connectionFactory, transactionFactory, connectionInformation, tableNameHelper,
                    tableProbe, upgradeLock, commandCache, logger)
            {
            }

            protected override void LoadVersions()
            {
                Versions.Add(1, new MarkerVersion());
            }
        }

        /// <summary>
        /// Creates one table, and would fail on a second application - which is the point. If the lock
        /// does not hold, the loser's CREATE TABLE errors rather than quietly doing nothing.
        /// </summary>
        private class MarkerVersion : ISchemaVersion
        {
            public string Script(ITableNameHelper tableNames, DbConnection connection,
                DbTransaction transaction)
            {
                return $"create table {tableNames.QueueName}UpgradeMarker (Id int not null primary key);";
            }
        }
    }
}
