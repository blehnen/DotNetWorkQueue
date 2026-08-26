using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    /// <summary>
    /// The async execution methods live on <see cref="SQLiteCommand"/>, not on
    /// <see cref="IDbCommand"/>, so <see cref="ReaderAsync"/> has to reach the provider command.
    /// A pooled connection hands out a wrapper, and this is what makes that work.
    /// </summary>
    [TestClass]
    public class ReaderAsyncTests
    {
        private string _dir;

        [TestInitialize]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "dnwq-readerasync", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            SQLiteConnection.ClearAllPools();
            try { Directory.Delete(_dir, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }

        private string NewDatabase()
        {
            var connectionString = $"Data Source={Path.Combine(_dir, "t.db")};Version=3;";
            using var seed = new SQLiteConnection(connectionString);
            seed.Open();
            using var cmd = seed.CreateCommand();
            cmd.CommandText = "CREATE TABLE t(id INTEGER PRIMARY KEY);";
            cmd.ExecuteNonQuery();
            return connectionString;
        }

        private static DbFactory CreateFactory()
        {
            var containerFactory = Substitute.For<IContainerFactory>();
            containerFactory.Create().Returns(Substitute.For<IContainer>());
            return new DbFactory(containerFactory);
        }

        [TestMethod]
        public async Task APooledCommandCanBeExecutedAsynchronously()
        {
            //regression guard: ReaderAsync used to cast straight to SQLiteCommand, which threw for
            //every async send once pooled connections started handing out a wrapper
            using var factory = CreateFactory();
            var connectionString = NewDatabase();
            using var connection = factory.CreateConnection(connectionString, false);
            var reader = new ReaderAsync();

            using (var insert = factory.CreateCommand(connection, "INSERT INTO t(id) VALUES (NULL)"))
            {
                Assert.AreEqual(1, await reader.ExecuteNonQueryAsync(insert));
            }

            using (var count = factory.CreateCommand(connection, "SELECT COUNT(*) FROM t"))
            {
                Assert.AreEqual(1L, Convert.ToInt64(await reader.ExecuteScalarAsync(count)));
            }

            using (var select = factory.CreateCommand(connection, "SELECT id FROM t"))
            using (var results = await reader.ExecuteReaderAsync(select))
            {
                Assert.IsTrue(results.Read());
            }
        }

        [TestMethod]
        public async Task APlainProviderCommandStillWorks()
        {
            //the unpooled path - in-memory databases hand out a real SQLiteConnection
            var connectionString = NewDatabase();
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM t";

            Assert.AreEqual(0L, Convert.ToInt64(await new ReaderAsync().ExecuteScalarAsync(command)));
        }

        [TestMethod]
        public async Task ACommandFromAnotherProvider_IsRejectedClearly()
        {
            var command = Substitute.For<IDbCommand>();

            var ex = await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => new ReaderAsync().ExecuteNonQueryAsync(command));

            StringAssert.Contains(ex.Message, "Expected a SQLite command");
        }
    }
}
