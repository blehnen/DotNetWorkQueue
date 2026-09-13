using System;
using System.Data.SQLite;
using System.IO;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Basic
{
    /// <summary>
    /// Which creation paths leave a database in WAL journal mode.
    ///
    /// This matters more than it looks. SQLite has no asynchronous I/O and the engine is never going to
    /// grow any, so write-ahead logging is the only concurrency lever the transport has, and the journal
    /// mode of a file is decided once - nothing converts an existing database later. A path that misses
    /// it produces a queue that is slower under load for the rest of its life, with nothing to show for
    /// it in a log.
    ///
    /// These tests deliberately do not use <see cref="IntegrationConnectionInfo"/>. That fixture runs
    /// "PRAGMA journal_mode=WAL" against the file before the library is handed the connection string, so
    /// every assertion here would pass on its own setup and a transport that had stopped setting the
    /// mode entirely would still look correct. Each test owns a raw file instead.
    /// </summary>
    [TestClass]
    public class WalJournalModeTests
    {
        [TestMethod]
        public void CreateQueue_SetsWalMode()
        {
            using (var db = new RawDatabase())
            {
                var queueConnection = new QueueConnection(GenerateQueueName.Create(), db.ConnectionString);
                using (var container = new QueueCreationContainer<SqLiteMessageQueueInit>())
                using (var creation = container.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection))
                {
                    var result = creation.CreateQueue();
                    Assert.AreEqual(QueueCreationStatus.Success, result.Status);
                }

                Assert.AreEqual("wal", db.JournalMode());
            }
        }

        [TestMethod]
        public void CreateJobSchedulerQueue_SetsWalMode()
        {
            using (var db = new RawDatabase())
            {
                var queueConnection = new QueueConnection(GenerateQueueName.Create(), db.ConnectionString);
                using (var container = new JobQueueCreationContainer<SqLiteMessageQueueInit>())
                using (var creation = container.GetQueueCreation<SqliteJobQueueCreation>(queueConnection))
                {
                    var result = creation.CreateJobSchedulerQueue(x => { }, queueConnection);
                    Assert.AreEqual(QueueCreationStatus.Success, result.Status);
                }

                Assert.AreEqual("wal", db.JournalMode());
            }
        }

        /// <summary>
        /// A job producer creates the database itself when it is the first thing to reach the file, so
        /// the job table path is a creation path and has to set the journal mode like the others. It did
        /// not, and this is the case that caught it: before the fix the file came back "delete".
        /// </summary>
        [TestMethod]
        public void JobProducer_ReachingADatabaseFirst_SetsWalMode()
        {
            using (var db = new RawDatabase())
            {
                var queueConnection = new QueueConnection(GenerateQueueName.Create(), db.ConnectionString);
                using (var container = new QueueContainer<SqLiteMessageQueueInit>())
                using (var producer = container.CreateMethodJobProducer(queueConnection))
                {
                    producer.Start();
                }

                Assert.AreEqual("wal", db.JournalMode());
            }
        }

        /// <summary>
        /// The control for the three above. Without it they would also pass against a database that was
        /// in WAL mode for some reason other than the transport choosing it.
        /// </summary>
        [TestMethod]
        public void CreateQueue_WithWalModeDisabled_LeavesTheJournalModeAlone()
        {
            using (var db = new RawDatabase())
            {
                var queueConnection = new QueueConnection(GenerateQueueName.Create(), db.ConnectionString);
                using (var container = new QueueCreationContainer<SqLiteMessageQueueInit>())
                using (var creation = container.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection))
                {
                    creation.Options.EnableWalMode = false;
                    var result = creation.CreateQueue();
                    Assert.AreEqual(QueueCreationStatus.Success, result.Status);
                }

                Assert.AreEqual("delete", db.JournalMode());
            }
        }

        /// <summary>
        /// A database file the library has not seen, and its clean-up. Nothing here touches the journal
        /// mode; the file is not even created, so that the transport decides both whether it exists and
        /// what mode it is in.
        /// </summary>
        private sealed class RawDatabase : IDisposable
        {
            private readonly string _fileName;

            public RawDatabase()
            {
                _fileName = Path.Combine(Path.GetTempPath(), GenerateQueueName.CreateFileName());
                ConnectionString = $"Data Source={_fileName};Version=3;";
            }

            public string ConnectionString { get; }

            public string JournalMode()
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "PRAGMA journal_mode;";
                        return ((string)command.ExecuteScalar()).ToLowerInvariant();
                    }
                }
            }

            public void Dispose()
            {
                //pooling keeps the file handle open, and WAL leaves two files beside the database
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
        }
    }
}
