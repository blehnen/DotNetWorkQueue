using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    /// <summary>
    /// The ADO surface that <see cref="PooledConnection"/> and <see cref="PooledCommand"/> inherit
    /// from <see cref="DbConnection"/> and <see cref="DbCommand"/>.
    ///
    /// These two used to implement the old <c>IDbConnection</c> and <c>IDbCommand</c> interfaces,
    /// which have no asynchronous members and so blocked any async path on the relational transports.
    /// Deriving from the base classes brought members with it - DataSource, ServerVersion,
    /// DesignTimeVisible, and the protected Create/Begin/Execute methods the public ones route
    /// through - and none of them were exercised by anything.
    ///
    /// The behaviour that matters is asserted alongside: a pooled command still refuses a different
    /// CommandText, and a pooled connection still refuses a different connection string. Those two
    /// refusals are what keep a command filed under a key that describes it, which is what makes the
    /// statement caching correct rather than merely fast.
    /// </summary>
    [TestClass]
    public class PooledAdoSurfaceTests
    {
        private const string Count = "SELECT COUNT(*) FROM t";
        private string _dir;

        [TestInitialize]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "dnwq-ado-surface", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            SQLiteConnection.ClearAllPools();
            try { Directory.Delete(_dir, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }

        [TestMethod]
        public void Connection_ExposesTheInnerConnectionsIdentity()
        {
            var factory = CreateFactory();
            using var connection = (PooledConnection)factory.CreateConnection(NewDatabase(), false);
            connection.Open();

            Assert.IsFalse(string.IsNullOrEmpty(connection.DataSource), "DataSource came back empty");
            Assert.IsFalse(string.IsNullOrEmpty(connection.ServerVersion), "ServerVersion came back empty");
            Assert.AreEqual(ConnectionState.Open, connection.State);
            Assert.IsTrue(connection.ConnectionTimeout >= 0);
        }

        [TestMethod]
        public void Connection_RefusesADifferentConnectionString()
        {
            var factory = CreateFactory();
            using var connection = (PooledConnection)factory.CreateConnection(NewDatabase(), false);

            Assert.Throws<NotSupportedException>(() => connection.ConnectionString = "Data Source=other;Version=3;");
        }

        [TestMethod]
        public void Connection_CreatesCommandsAndTransactionsThroughTheBase()
        {
            var factory = CreateFactory();
            using var connection = (PooledConnection)factory.CreateConnection(NewDatabase(), false);
            connection.Open();

            //Both of these route through the protected CreateDbCommand and BeginDbTransaction.
            using var command = connection.CreateCommand();
            Assert.IsNotNull(command);

            using var transaction = connection.BeginTransaction();
            Assert.IsNotNull(transaction);
            transaction.Rollback();
        }

        [TestMethod]
        public void Command_ExposesTheAdoSurfaceItInherits()
        {
            var factory = CreateFactory();
            using var connection = (PooledConnection)factory.CreateConnection(NewDatabase(), false);
            connection.Open();
            using var command = factory.CreateCommand(connection, Count);

            command.DesignTimeVisible = true;
            Assert.IsTrue(command.DesignTimeVisible);

            command.CommandType = CommandType.Text;
            Assert.AreEqual(CommandType.Text, command.CommandType);

            command.UpdatedRowSource = UpdateRowSource.None;
            Assert.AreEqual(UpdateRowSource.None, command.UpdatedRowSource);

            Assert.IsNotNull(command.Parameters);
            Assert.IsNotNull(command.CreateParameter());
            //Connection hands back the underlying SQLiteConnection rather than the pooled
            //wrapper, which is what it did before this class derived from DbCommand. Asserted
            //because it is surprising, not because it is desirable.
            Assert.IsInstanceOfType<SQLiteConnection>(command.Connection);
            Assert.AreNotSame(connection, command.Connection);

            command.CommandTimeout = 42;
            Assert.AreEqual(42, command.CommandTimeout);
        }

        [TestMethod]
        public void Command_ReadsThroughTheProtectedExecuteReader()
        {
            var factory = CreateFactory();
            using var connection = (PooledConnection)factory.CreateConnection(NewDatabase(), false);
            connection.Open();
            using var command = factory.CreateCommand(connection, Count);

            //The public ExecuteReader is not virtual; it routes through ExecuteDbDataReader, which is
            //the member the pooled command actually overrides.
            using var reader = command.ExecuteReader();

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(0, reader.GetInt32(0));
        }

        [TestMethod]
        public void Command_RefusesADifferentCommandTextButAcceptsTheSameOne()
        {
            var factory = CreateFactory();
            using var connection = (PooledConnection)factory.CreateConnection(NewDatabase(), false);
            connection.Open();
            using var command = factory.CreateCommand(connection, Count);

            //Re-assigning the text it already holds is the normal shape of the callers and must not
            //throw; a different text would leave it filed under a key that no longer describes it.
            command.CommandText = Count;
            Assert.AreEqual(Count, command.CommandText);

            Assert.Throws<NotSupportedException>(() => command.CommandText = "SELECT 1");
        }

        [TestMethod]
        public void Command_RefusesADifferentConnection()
        {
            var factory = CreateFactory();
            using var connection = (PooledConnection)factory.CreateConnection(NewDatabase(), false);
            connection.Open();
            using var command = factory.CreateCommand(connection, Count);

            Assert.Throws<NotSupportedException>(() => command.Connection = null);
        }

        [TestMethod]
        public void Connection_CloseIsANoOp_SoThePooledConnectionStaysUsable()
        {
            var factory = CreateFactory();
            using var connection = (PooledConnection)factory.CreateConnection(NewDatabase(), false);
            connection.Open();

            //Close deliberately does nothing. Closing the inner connection would defeat the pool and
            //throw away the statements SQLite compiled for its commands, which is the whole point of
            //this type. Callers that dispose or close should get a connection that still works.
            connection.Close();

            Assert.AreEqual(ConnectionState.Open, connection.State);
            using var command = factory.CreateCommand(connection, Count);
            Assert.AreEqual(0L, (long)command.ExecuteScalar());
        }

        [TestMethod]
        public void Connection_OpeningTwiceIsHarmless()
        {
            var factory = CreateFactory();
            using var connection = (PooledConnection)factory.CreateConnection(NewDatabase(), false);

            connection.Open();
            connection.Open();

            Assert.AreEqual(ConnectionState.Open, connection.State);
        }

        [TestMethod]
        public void Connection_ReportsTheStringItWasRentedFor()
        {
            var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);
            connection.Open();

            //Not the string that was passed in: the factory normalises it before renting, so what
            //comes back describes the same database rather than matching character for character.
            Assert.IsFalse(string.IsNullOrEmpty(connection.ConnectionString));
            StringAssert.Contains(connection.ConnectionString, "t.db");
            Assert.IsNotNull(connection.Database);
        }

        [TestMethod]
        public void Connection_ChangeDatabaseReachesTheInnerConnection()
        {
            var factory = CreateFactory();
            using var connection = (PooledConnection)factory.CreateConnection(NewDatabase(), false);
            connection.Open();

            //System.Data.SQLite does not implement this, so the call is expected to fail rather than
            //succeed. What is asserted is that it reaches the inner connection rather than being
            //swallowed - a silent no-op here would hide a caller's mistake.
            Assert.ThrowsExactly<NotImplementedException>(() => connection.ChangeDatabase("other"));
        }

        private static DbFactory CreateFactory()
        {
            var containerFactory = Substitute.For<IContainerFactory>();
            containerFactory.Create().Returns(Substitute.For<IContainer>());
            return new DbFactory(containerFactory);
        }

        private string NewDatabase(string name = "t.db")
        {
            var path = Path.Combine(_dir, name);
            var connectionString = $"Data Source={path};Version=3;";
            using var seed = new SQLiteConnection(connectionString);
            seed.Open();
            using var cmd = seed.CreateCommand();
            cmd.CommandText = "CREATE TABLE t(id INTEGER PRIMARY KEY);";
            cmd.ExecuteNonQuery();
            return connectionString;
        }
    }
}
