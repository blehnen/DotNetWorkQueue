using System;
using System.Data.SQLite;
using System.IO;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Decorator;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Decorator
{
    /// <summary>
    /// Setting the journal mode, and what happens when it cannot be set.
    ///
    /// The failure case is the reason this is a separate class rather than a line inside the
    /// decorators. SQLite refuses to convert a database another connection is holding, and it refuses
    /// by throwing. Letting that escape would fail a queue creation whose tables had already been
    /// committed, and the caller's retry would then find those tables, return AlreadyExists, and never
    /// reach the conversion again - so one moment of contention would cost the database its journal
    /// mode permanently.
    /// </summary>
    [TestClass]
    public class WalJournalModeTests
    {
        [TestMethod]
        public void Apply_SetsWalMode()
        {
            using (var db = new TempDatabase())
            {
                WalJournalMode.Apply(db.ConnectionInformation, db.FileNameParser, Options(true), Substitute.For<ILogger>());

                Assert.AreEqual("wal", db.JournalMode());
            }
        }

        [TestMethod]
        public void Apply_WhenDisabled_LeavesTheJournalModeAlone()
        {
            using (var db = new TempDatabase())
            {
                WalJournalMode.Apply(db.ConnectionInformation, db.FileNameParser, Options(false), Substitute.For<ILogger>());

                Assert.AreEqual("delete", db.JournalMode());
            }
        }

        /// <summary>
        /// The case that matters: another connection is holding the database, so the conversion cannot
        /// happen. Creation has to survive that - the tables are what the caller asked for, and the
        /// journal mode is a performance setting applied on top of them.
        /// </summary>
        [TestMethod]
        public void Apply_WhenAnotherConnectionHoldsTheDatabase_DoesNotThrow()
        {
            using (var db = new TempDatabase())
            {
                var logger = Substitute.For<ILogger>();

                using (db.HoldExclusively())
                {
                    WalJournalMode.Apply(db.ConnectionInformation, db.FileNameParser, Options(true), logger);
                }

                //the database is still usable, just not in the mode that was asked for, and the
                //warning is the only trace of that
                Assert.AreEqual("delete", db.JournalMode());
                logger.ReceivedWithAnyArgs().Log(LogLevel.Warning, default, default(object), null, default!);
            }
        }

        [TestMethod]
        public void Apply_OnAnInMemoryDatabase_DoesNothing()
        {
            var connectionInformation = Substitute.For<IConnectionInformation>();
            connectionInformation.ConnectionString.Returns("FullUri=file:memtest?mode=memory&cache=shared;Version=3;");
            var parser = Substitute.For<IGetFileNameFromConnectionString>();
            parser.GetFileName(Arg.Any<string>()).Returns(new ConnectionStringInfo(true, string.Empty));
            var logger = Substitute.For<ILogger>();

            WalJournalMode.Apply(connectionInformation, parser, Options(true), logger);

            logger.DidNotReceiveWithAnyArgs().Log(LogLevel.Warning, default, default(object), null, default!);
        }

        private static ISqLiteMessageQueueTransportOptionsFactory Options(bool enableWalMode)
        {
            var options = new SqLiteMessageQueueTransportOptions { EnableWalMode = enableWalMode };
            var factory = Substitute.For<ISqLiteMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(options);
            return factory;
        }

        /// <summary>
        /// A real database file, in the default rollback-journal mode, with a table in it so that
        /// holding it exclusively is the same situation a live queue would produce.
        /// </summary>
        private sealed class TempDatabase : IDisposable
        {
            private readonly string _fileName;

            public TempDatabase()
            {
                _fileName = Path.Combine(Path.GetTempPath(), $"wal{Guid.NewGuid():N}.db3");
                var connectionString = $"Data Source={_fileName};Version=3;";

                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "CREATE TABLE t(id INTEGER PRIMARY KEY);";
                        command.ExecuteNonQuery();
                    }
                }

                ConnectionInformation = Substitute.For<IConnectionInformation>();
                ConnectionInformation.ConnectionString.Returns(connectionString);

                FileNameParser = Substitute.For<IGetFileNameFromConnectionString>();
                FileNameParser.GetFileName(Arg.Any<string>()).Returns(new ConnectionStringInfo(false, _fileName));
            }

            public IConnectionInformation ConnectionInformation { get; }

            public IGetFileNameFromConnectionString FileNameParser { get; }

            public string JournalMode()
            {
                using (var connection = new SQLiteConnection(ConnectionInformation.ConnectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "PRAGMA journal_mode;";
                        return ((string)command.ExecuteScalar()).ToLowerInvariant();
                    }
                }
            }

            /// <summary>
            /// Takes the write lock and keeps it until disposed, which is what makes SQLite refuse a
            /// journal mode change.
            /// </summary>
            public IDisposable HoldExclusively()
            {
                var connection = new SQLiteConnection(ConnectionInformation.ConnectionString);
                connection.Open();
                using (var begin = connection.CreateCommand())
                {
                    begin.CommandText = "BEGIN EXCLUSIVE;";
                    begin.ExecuteNonQuery();
                }

                return new Lock(connection);
            }

            public void Dispose()
            {
                SQLiteConnection.ClearAllPools();
                foreach (var file in new[] { _fileName, _fileName + "-wal", _fileName + "-shm" })
                {
                    try
                    {
                        if (File.Exists(file))
                            File.Delete(file);
                    }
                    catch (IOException)
                    {
                        //a leaked temp file is not worth failing a test over
                    }
                }
            }

            private sealed class Lock : IDisposable
            {
                private readonly SQLiteConnection _connection;

                public Lock(SQLiteConnection connection)
                {
                    _connection = connection;
                }

                public void Dispose()
                {
                    using (var rollback = _connection.CreateCommand())
                    {
                        rollback.CommandText = "ROLLBACK;";
                        rollback.ExecuteNonQuery();
                    }

                    _connection.Dispose();
                }
            }
        }
    }
}
