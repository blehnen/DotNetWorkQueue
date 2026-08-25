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
    /// Covers the command reuse in <see cref="PooledConnectionEntry"/>. SQLite compiles a command's
    /// statements on first execution and keeps them on the command object, so reusing the object is
    /// what avoids recompiling; measured against an empty queue, a dequeue went from 27,760 ns and
    /// 22,144 B to 6,021 ns and 7,496 B.
    /// </summary>
    [TestClass]
    public class PooledCommandTests
    {
        private const string Insert = "INSERT INTO t(id) VALUES (NULL)";
        private const string Count = "SELECT COUNT(*) FROM t";

        private string _dir;

        [TestInitialize]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "dnwq-command-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            SQLiteConnection.ClearAllPools();
            try { Directory.Delete(_dir, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
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

        [TestMethod]
        public void TheSameTextTwice_KeepsOneCommand()
        {
            using var factory = CreateFactory();
            var connectionString = NewDatabase();

            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);
            factory.CreateCommand(connection, Insert).Dispose();
            factory.CreateCommand(connection, Insert).Dispose();

            Assert.AreEqual(1, connection.CachedCommandCount,
                "the second request should have reused the command compiled for the first");
        }

        [TestMethod]
        public void DifferentTexts_GetDifferentCommands()
        {
            using var factory = CreateFactory();
            var connectionString = NewDatabase();

            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);
            factory.CreateCommand(connection, Insert).Dispose();
            factory.CreateCommand(connection, Count).Dispose();

            Assert.AreEqual(2, connection.CachedCommandCount);
        }

        [TestMethod]
        public void ACommandSurvivesTheConnectionGoingBackToThePool()
        {
            //the point of hanging the cache off the pooled connection rather than off the caller
            using var factory = CreateFactory();
            var connectionString = NewDatabase();

            using (var first = (PooledConnection)factory.CreateConnection(connectionString, false))
            {
                factory.CreateCommand(first, Insert).Dispose();
            }

            using var second = (PooledConnection)factory.CreateConnection(connectionString, false);
            Assert.AreEqual(1, second.CachedCommandCount,
                "renting the connection again should have brought its compiled commands with it");
        }

        [TestMethod]
        public void ParametersDoNotCarryOverBetweenCallers()
        {
            //a caller adds its own parameters, so it must be handed an empty collection
            using var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);

            using (var first = factory.CreateCommand(connection, "SELECT @a"))
            {
                var parameter = first.CreateParameter();
                parameter.ParameterName = "@a";
                parameter.Value = 1;
                first.Parameters.Add(parameter);
            }

            using var second = factory.CreateCommand(connection, "SELECT @a");
            Assert.AreEqual(0, second.Parameters.Count);
        }

        [TestMethod]
        public void AReusedCommandStillExecutes()
        {
            using var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);

            for (var i = 0; i < 5; i++)
            {
                using var command = factory.CreateCommand(connection, Insert);
                command.ExecuteNonQuery();
            }

            using var count = factory.CreateCommand(connection, Count);
            Assert.AreEqual(5L, Convert.ToInt64(count.ExecuteScalar()));
        }

        [TestMethod]
        public void ChangingTheTextOfAPooledCommand_IsRefused()
        {
            //it would leave the command filed under a key that no longer describes it
            using var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);

            using var command = factory.CreateCommand(connection, Insert);

            Assert.ThrowsExactly<NotSupportedException>(() => command.CommandText = Count);
        }

        [TestMethod]
        public void SettingTheSameTextAgain_IsAllowed()
        {
            //callers set the text unconditionally; that must not throw, and must not recompile
            using var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);

            using var command = factory.CreateCommand(connection, Insert);
            command.CommandText = Insert;

            Assert.AreEqual(Insert, command.CommandText);
        }

        [TestMethod]
        public void ACommandAlreadyInUse_IsNotHandedOutTwice()
        {
            //a caller holding a reader open while asking for another command must not be given the
            //command it is already using
            using var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);

            using var held = factory.CreateCommand(connection, Insert);
            using var second = factory.CreateCommand(connection, Insert);

            second.ExecuteNonQuery();
            held.ExecuteNonQuery();

            using var count = factory.CreateCommand(connection, Count);
            Assert.AreEqual(2L, Convert.ToInt64(count.ExecuteScalar()));
        }

        [TestMethod]
        public void TheNumberOfCachedCommandsIsCapped()
        {
            //the dequeue script embeds the caller's user clause, which a caller could vary per call
            using var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);

            for (var i = 0; i < 40; i++)
                factory.CreateCommand(connection, $"SELECT {i}").Dispose();

            Assert.AreEqual(16, connection.CachedCommandCount);
        }

        [TestMethod]
        public void BeyondTheCap_CommandsStillWork()
        {
            using var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);

            for (var i = 0; i < 40; i++)
                factory.CreateCommand(connection, $"SELECT {i}").Dispose();

            using var command = factory.CreateCommand(connection, "SELECT 99");
            Assert.AreEqual(99L, Convert.ToInt64(command.ExecuteScalar()));
        }

        [TestMethod]
        public void DisposingTheFactory_ReleasesTheDatabaseFileEvenWithCommandsCached()
        {
            //a command outliving its connection is what leaves a file locked
            var path = Path.Combine(_dir, "release.db");
            var connectionString = $"Data Source={path};Version=3;";
            using (var seed = new SQLiteConnection(connectionString)) { seed.Open(); }

            var factory = CreateFactory();
            using (var connection = (PooledConnection)factory.CreateConnection(connectionString, false))
            {
                factory.CreateCommand(connection, "SELECT 1").Dispose();
            }
            factory.Dispose();
            SQLiteConnection.ClearAllPools();

            File.Delete(path);
            Assert.IsFalse(File.Exists(path));
        }

        [TestMethod]
        public void AnUnpooledConnection_StillGetsAWorkingCommand()
        {
            //in-memory databases are handed out as plain connections; the factory default applies
            using var factory = CreateFactory();

            using var connection = factory.CreateConnection("Data Source=:memory:;Version=3;", false);
            connection.Open();
            using var command = factory.CreateCommand(connection, "SELECT 7");

            Assert.AreEqual(7L, Convert.ToInt64(command.ExecuteScalar()));
        }
    }
}
