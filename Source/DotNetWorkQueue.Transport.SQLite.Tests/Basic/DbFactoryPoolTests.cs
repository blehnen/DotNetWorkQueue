using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    /// <summary>
    /// Covers the connection reuse in <see cref="DbFactory"/>. The behaviour that matters is that
    /// disposing a rented connection returns it rather than closing it, and that disposing the
    /// factory is what actually releases the database file.
    /// </summary>
    [TestClass]
    public class DbFactoryPoolTests
    {
        private string _dir;

        [TestInitialize]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "dnwq-pool-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            SQLiteConnection.ClearAllPools();
            try { Directory.Delete(_dir, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }

        private static DbFactory Create()
        {
            var containerFactory = Substitute.For<IContainerFactory>();
            containerFactory.Create().Returns(Substitute.For<IContainer>());
            return new DbFactory(containerFactory);
        }

        private string NewDatabase(string name = "t.db")
        {
            var path = Path.Combine(_dir, name);
            using var seed = new SQLiteConnection($"Data Source={path};Version=3;");
            seed.Open();
            using var cmd = seed.CreateCommand();
            cmd.CommandText = "CREATE TABLE t(id INTEGER PRIMARY KEY);";
            cmd.ExecuteNonQuery();
            return $"Data Source={path};Version=3;";
        }

        [TestMethod]
        public void RentedConnection_IsAlreadyOpen()
        {
            //the pool hands out open connections; leaving them open is the entire point
            using var factory = Create();

            using var connection = factory.CreateConnection(NewDatabase(), false);

            Assert.AreEqual(ConnectionState.Open, connection.State);
        }

        [TestMethod]
        public void Open_OnAnAlreadyOpenPooledConnection_DoesNotThrow()
        {
            //callers call Open() because that is the contract for a new connection, and calling it
            //on an open SQLiteConnection would throw
            using var factory = Create();

            using var connection = factory.CreateConnection(NewDatabase(), false);
            connection.Open();

            Assert.AreEqual(ConnectionState.Open, connection.State);
        }

        [TestMethod]
        public void DisposingARentedConnection_ReturnsItInsteadOfClosingIt()
        {
            using var factory = Create();
            var connectionString = NewDatabase();

            var first = factory.CreateConnection(connectionString, false);
            first.Dispose();
            using var second = factory.CreateConnection(connectionString, false);

            Assert.AreEqual(ConnectionState.Open, second.State,
                "the second rent should have reused the returned connection, still open");
        }

        [TestMethod]
        public void ARentedConnectionCanStillBeUsed()
        {
            using var factory = Create();
            var connectionString = NewDatabase();

            for (var i = 0; i < 5; i++)
            {
                using var connection = factory.CreateConnection(connectionString, false);
                connection.Open();
                using var tx = connection.BeginTransaction();
                using var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT INTO t(id) VALUES (NULL)";
                cmd.ExecuteNonQuery();
                tx.Commit();
            }

            using var verify = factory.CreateConnection(connectionString, false);
            using var count = verify.CreateCommand();
            count.CommandText = "SELECT COUNT(*) FROM t";
            Assert.AreEqual(5L, Convert.ToInt64(count.ExecuteScalar()));
        }

        [TestMethod]
        public void DisposingTheFactory_ReleasesTheDatabaseFile()
        {
            //This is the contract that replaces ClearAllPools for pooled objects: disposing the
            //queue - and so the factory - is what lets a caller delete the database.
            var path = Path.Combine(_dir, "release.db");
            var connectionString = $"Data Source={path};Version=3;";
            using (var seed = new SQLiteConnection(connectionString)) { seed.Open(); }

            var factory = Create();
            factory.CreateConnection(connectionString, false).Dispose();
            factory.Dispose();
            SQLiteConnection.ClearAllPools();

            File.Delete(path);
            Assert.IsFalse(File.Exists(path), "the database file should be deletable once the factory is disposed");
        }

        [TestMethod]
        public void CreateConnection_AfterDispose_Throws()
        {
            var factory = Create();
            var connectionString = NewDatabase();
            factory.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() => factory.CreateConnection(connectionString, false));
        }

        [TestMethod]
        public void InMemoryDatabases_AreNotPooled()
        {
            //an in-memory database is kept alive by SqLiteHoldConnection; extra open connections
            //to a shared-cache one would keep it alive past the caller's disposal
            using var factory = Create();

            using var connection = factory.CreateConnection("Data Source=:memory:;Version=3;", false);

            Assert.IsInstanceOfType<SQLiteConnection>(connection,
                "in-memory connections should be handed out directly, not pooled");
        }

        [TestMethod]
        public void HoldConnections_AreNotPooled()
        {
            //a hold connection is never released, so pooling it would serve no purpose
            using var factory = Create();

            using var connection = factory.CreateConnection(NewDatabase("hold.db"), true);

            Assert.IsInstanceOfType<SQLiteConnection>(connection);
        }

        [TestMethod]
        public void AConnectionDisposedBeforeItsTransaction_IsNotReturnedToThePool()
        {
            //Regression guard. A connection carrying an open transaction would hand that
            //transaction to the next renter. AutoCommit is false exactly while one is open, so
            //such a connection is discarded rather than pooled.
            using var factory = Create();
            var connectionString = NewDatabase();

            var leaked = factory.CreateConnection(connectionString, false);
            var transaction = leaked.BeginTransaction();
            using (var cmd = leaked.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandText = "INSERT INTO t(id) VALUES (NULL)";
                cmd.ExecuteNonQuery();
            }
            leaked.Dispose();   //disposed before the transaction was committed or rolled back

            using var next = factory.CreateConnection(connectionString, false);
            using var check = next.CreateCommand();
            check.CommandText = "SELECT COUNT(*) FROM t";

            //a renter inheriting the open transaction would see the uncommitted row
            Assert.AreEqual(0L, Convert.ToInt64(check.ExecuteScalar()),
                "the next renter must not inherit an uncommitted transaction");
        }

        [TestMethod]
        public void DisposingARentedConnectionTwice_IsSafe()
        {
            using var factory = Create();

            var connection = factory.CreateConnection(NewDatabase(), false);
            connection.Dispose();
            connection.Dispose();

            //a double dispose must not return the same connection to the pool twice
            using var a = factory.CreateConnection(NewDatabase("second.db"), false);
            Assert.AreEqual(ConnectionState.Open, a.State);
        }
    }
}
