using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.SQLite.Basic;
using FluentAssertions;
using Xunit;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    /// <summary>
    /// Covers the command reuse in <see cref="PooledConnectionEntry"/>. SQLite compiles a command's
    /// statements on first execution and keeps them on the command object, so reusing the object is
    /// what avoids recompiling; measured against an empty queue, a dequeue went from 27,760 ns and
    /// 22,144 B to 6,021 ns and 7,496 B.
    /// </summary>
    public class PooledCommandTests : IDisposable
    {
        private const string Insert = "INSERT INTO t(id) VALUES (NULL)";
        private const string Count = "SELECT COUNT(*) FROM t";

        private string _dir;

        public PooledCommandTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "dnwq-command-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
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

        [Fact]
        public void TheSameTextTwice_KeepsOneCommand()
        {
            using var factory = CreateFactory();
            var connectionString = NewDatabase();

            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);
            factory.CreateCommand(connection, Insert).Dispose();
            factory.CreateCommand(connection, Insert).Dispose();

            connection.CachedCommandCount.Should().Be(1, "the second request should have reused the command compiled for the first");
        }

        [Fact]
        public void DifferentTexts_GetDifferentCommands()
        {
            using var factory = CreateFactory();
            var connectionString = NewDatabase();

            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);
            factory.CreateCommand(connection, Insert).Dispose();
            factory.CreateCommand(connection, Count).Dispose();

            Assert.Equal(2, connection.CachedCommandCount);
        }

        [Fact]
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
            second.CachedCommandCount.Should().Be(1, "renting the connection again should have brought its compiled commands with it");
        }

        [Fact]
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
            Assert.Empty(second.Parameters);
        }

        [Fact]
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
            Assert.Equal(5L, Convert.ToInt64(count.ExecuteScalar()));
        }

        [Fact]
        public void ChangingTheTextOfAPooledCommand_IsRefused()
        {
            //it would leave the command filed under a key that no longer describes it
            using var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);

            using var command = factory.CreateCommand(connection, Insert);

            Assert.Throws<NotSupportedException>(() => command.CommandText = Count);
        }

        [Fact]
        public void SettingTheSameTextAgain_IsAllowed()
        {
            //callers set the text unconditionally; that must not throw, and must not recompile
            using var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);

            using var command = factory.CreateCommand(connection, Insert);
            command.CommandText = Insert;

            Assert.Equal(Insert, command.CommandText);
        }

        [Fact]
        public void ACommandAlreadyInUse_IsNotHandedOutTwice()
        {
            //a caller holding a reader open while asking for another command must not be given the
            //command it is already using
            using var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);

            using var held = factory.CreateCommand(connection, Insert);
            var marker = held.CreateParameter();
            marker.ParameterName = "@unused";
            marker.Value = 1;
            held.Parameters.Add(marker);

            using var second = factory.CreateCommand(connection, Insert);

            //if the pool had handed the same physical command out twice, they would share this
            second.Parameters.Count.Should().Be(0,
                "the second request must not have been given the command already in use");

            second.ExecuteNonQuery();

            using var count = factory.CreateCommand(connection, Count);
            Assert.Equal(1L, Convert.ToInt64(count.ExecuteScalar()));
        }

        [Fact]
        public void SettingsDoNotCarryOverBetweenCallers()
        {
            //a caller is free to change these; the next one must not inherit them
            using var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);

            int defaultTimeout;
            UpdateRowSource defaultRowSource;
            using (var first = factory.CreateCommand(connection, Count))
            {
                defaultTimeout = first.CommandTimeout;
                defaultRowSource = first.UpdatedRowSource;

                first.CommandTimeout = defaultTimeout + 120;
                first.UpdatedRowSource = UpdateRowSource.Both;
            }

            using var second = factory.CreateCommand(connection, Count);

            Assert.Equal(defaultTimeout, second.CommandTimeout);
            Assert.Equal(defaultRowSource, second.UpdatedRowSource);
        }

        [Fact]
        public void TheNumberOfCachedCommandsIsCapped()
        {
            //the dequeue script embeds the caller's user clause, which a caller could vary per call
            using var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);

            for (var i = 0; i < 40; i++)
                factory.CreateCommand(connection, $"SELECT {i}").Dispose();

            Assert.Equal(16, connection.CachedCommandCount);
        }

        [Fact]
        public void BeyondTheCap_CommandsStillWork()
        {
            using var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);

            for (var i = 0; i < 40; i++)
                factory.CreateCommand(connection, $"SELECT {i}").Dispose();

            using var command = factory.CreateCommand(connection, "SELECT 99");
            Assert.Equal(99L, Convert.ToInt64(command.ExecuteScalar()));
        }

        [Fact]
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
            Assert.False(File.Exists(path));
        }

        [Fact]
        public void TheWrapperDelegatesTheRestOfTheCommand()
        {
            //PooledCommand stands in for the real command everywhere, not just where it intervenes
            using var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);

            using var command = factory.CreateCommand(connection, Count);

            Assert.Equal(CommandType.Text, command.CommandType);
            Assert.NotNull(command.Connection);
            Assert.NotNull(command.Parameters);
            Assert.Null(command.Transaction);

            command.CommandType = CommandType.Text;
            command.CommandTimeout = 45;
            Assert.Equal(45, command.CommandTimeout);

            command.UpdatedRowSource = UpdateRowSource.None;
            Assert.Equal(UpdateRowSource.None, command.UpdatedRowSource);

            //a no-op here; statements are compiled on execution, not on Prepare
            command.Prepare();
            command.Cancel();

            using (var reader = command.ExecuteReader(CommandBehavior.SingleRow))
            {
                Assert.True(reader.Read());
            }

            Assert.Equal(0L, Convert.ToInt64(command.ExecuteScalar()));
        }

        [Fact]
        public void TheConnectionOfAPooledCommand_CannotBeChanged()
        {
            //it would leave the command filed against a connection that no longer owns it
            using var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);

            using var command = factory.CreateCommand(connection, Count);

            Assert.Throws<NotSupportedException>(() => command.Connection = null);
        }

        [Fact]
        public void ACommandUsedInsideATransaction_ComesBackDetached()
        {
            using var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);

            using (var transaction = connection.BeginTransaction())
            using (var command = factory.CreateCommand(connection, Insert))
            {
                command.Transaction = transaction;
                command.ExecuteNonQuery();
                transaction.Commit();
            }

            using var next = factory.CreateCommand(connection, Insert);
            next.Transaction.Should().BeNull("a committed transaction must not follow the command to its next caller");
        }

        [Fact]
        public void UsingAPooledCommandAfterDisposal_IsRefused()
        {
            using var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);

            var command = factory.CreateCommand(connection, Count);
            command.Dispose();

            Assert.Throws<ObjectDisposedException>(() => command.ExecuteScalar());
        }

        [Fact]
        public void DisposingAPooledCommandTwice_IsSafe()
        {
            using var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = (PooledConnection)factory.CreateConnection(connectionString, false);

            var command = factory.CreateCommand(connection, Count);
            command.Dispose();
            command.Dispose();

            //a double dispose must not release the same command to the pool twice
            using var next = factory.CreateCommand(connection, Count);
            Assert.Equal(0L, Convert.ToInt64(next.ExecuteScalar()));
        }

        [Fact]
        public void AnUnpooledConnection_StillGetsAWorkingCommand()
        {
            //in-memory databases are handed out as plain connections; the factory default applies
            using var factory = CreateFactory();

            using var connection = factory.CreateConnection("Data Source=:memory:;Version=3;", false);
            connection.Open();
            using var command = factory.CreateCommand(connection, "SELECT 7");

            Assert.Equal(7L, Convert.ToInt64(command.ExecuteScalar()));
        }
    }
}
