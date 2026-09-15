using System;
using System.Data.SQLite;
using System.IO;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Basic
{
    /// <summary>
    /// The upgrade script published for 0.12.0, run against a queue on the 0.11.0 schema.
    ///
    /// It runs the file that ships in docs/upgrade/0.12.0 rather than a copy of it, because a copy
    /// proves nothing about what an operator is handed - the two drift the moment either is edited
    /// alone, and the test keeps passing.
    ///
    /// The older queue is built rather than converted: WAL mode is off at creation, which is what a
    /// database made by 0.11.0 looks like, and the unique index is dropped afterwards. Converting a live
    /// database's journal mode back the other way does not work, and that is not a quirk of the test -
    /// SQLite wants the database to itself for that change in either direction, which is why the
    /// procedure says to stop everything using the file first. Disposing the creation container and
    /// clearing the connection pool is that step.
    ///
    /// GitHub #321.
    /// </summary>
    [TestClass]
    [Retry(2)]
    public class UpgradeScript0120Tests
    {
        [TestMethod]
        public void TheScript_BringsAnOlderQueueUpToDate()
        {
            using (var connectionInfo = new RawDatabase())
            {
                var queueName = GenerateQueueName.Create();
                var connectionString = connectionInfo.ConnectionString;
                var queueConnection = new QueueConnection(queueName, connectionString);
                var errorTable = $"{queueName}ErrorTracking";
                var logProvider = LoggerShared.Create(queueName, GetType().Name);

                //--- a queue as 0.11.0 would have left it ---
                using (var container = new QueueCreationContainer<SqLiteMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                using (var creation = container.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection))
                {
                    creation.Options.EnableWalMode = false;   //0.11.0 never set the journal mode
                    Assert.IsTrue(creation.CreateQueue().Success);

                    Execute(connectionString, $"DROP INDEX IX_QueueIDExceptionType{errorTable}");

                    //the duplicate rows the check-then-write race produced on 0.11.0: two workers failing
                    //the same message both inserted, and the count then read low from either row
                    Insert(connectionString, errorTable, 1, "System.Exception", 2);
                    Insert(connectionString, errorTable, 1, "System.Exception", 3);
                    Insert(connectionString, errorTable, 2, "System.InvalidOperationException", 1);
                    Insert(connectionString, errorTable, 3, "System.Exception", 4);
                    Insert(connectionString, errorTable, 3, "System.Exception", 1);
                    Insert(connectionString, errorTable, 3, "System.Exception", 2);
                }

                //without these the assertions after the upgrade would pass against a queue that was
                //already correct, and the script would not have been tested at all
                Assert.AreEqual("delete", JournalMode(connectionString),
                    "the database was not in the older journal mode, so step 2 has nothing to prove");
                Assert.IsFalse(UniqueIndexFound(queueConnection, logProvider, errorTable),
                    "the unique index was present before the upgrade, so step 1 has nothing to prove");

                //--- the upgrade, exactly as an operator runs it ---
                Quiesce();
                Execute(connectionString, UpgradeScript.Read("sqlite.sql", queueName));

                //--- step 1 ---
                Assert.IsTrue(UniqueIndexFound(queueConnection, logProvider, errorTable),
                    "the queue still reports no unique index, so it would keep using the racy count");

                Assert.AreEqual(1, RowCount(connectionString, errorTable, 1, "System.Exception"));
                Assert.AreEqual(1, RowCount(connectionString, errorTable, 3, "System.Exception"));

                //summed, not discarded - each row counted attempts that really happened, and a message
                //that loses them silently gets more retries than it is configured for
                Assert.AreEqual(5, RetryCount(connectionString, errorTable, 1, "System.Exception"),
                    "the retry counts of the collapsed rows were not preserved");
                Assert.AreEqual(7, RetryCount(connectionString, errorTable, 3, "System.Exception"),
                    "the retry counts of the collapsed rows were not preserved");

                //a row that was never duplicated has to come through untouched
                Assert.AreEqual(1, RetryCount(connectionString, errorTable, 2, "System.InvalidOperationException"),
                    "a row with no duplicates was changed by the collapse");

                //--- step 2 ---
                Assert.AreEqual("wal", JournalMode(connectionString),
                    "the database is still in the older journal mode, so readers and writers still take turns");
            }
        }

        [TestMethod]
        public void TheScript_CanBeRunTwice()
        {
            //an operator who is unsure whether it has already been run has to be able to just run it
            using (var connectionInfo = new RawDatabase())
            {
                var queueName = GenerateQueueName.Create();
                var connectionString = connectionInfo.ConnectionString;
                var queueConnection = new QueueConnection(queueName, connectionString);
                var errorTable = $"{queueName}ErrorTracking";
                var logProvider = LoggerShared.Create(queueName, GetType().Name);

                using (var container = new QueueCreationContainer<SqLiteMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                using (var creation = container.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection))
                {
                    creation.Options.EnableWalMode = false;
                    Assert.IsTrue(creation.CreateQueue().Success);

                    Execute(connectionString, $"DROP INDEX IX_QueueIDExceptionType{errorTable}");
                    Insert(connectionString, errorTable, 1, "System.Exception", 2);
                    Insert(connectionString, errorTable, 1, "System.Exception", 3);
                }

                var script = UpgradeScript.Read("sqlite.sql", queueName);
                Quiesce();
                Execute(connectionString, script);
                Quiesce();
                Execute(connectionString, script);

                Assert.AreEqual(1, RowCount(connectionString, errorTable, 1, "System.Exception"));
                //the second run must not sum an already-summed value again
                Assert.AreEqual(5, RetryCount(connectionString, errorTable, 1, "System.Exception"),
                    "running the script twice changed the retry count");
                Assert.AreEqual("wal", JournalMode(connectionString));
            }
        }

        [TestMethod]
        public void TheScript_RefusesToTouchAnIndexBelongingToAnotherTable()
        {
            //Index names in SQLite are database-wide rather than scoped to a table, so the generated
            //name existing does not prove it belongs to this queue. The script recreates the index to
            //guarantee its shape, and recreating means dropping - which would take somebody else's
            //uniqueness constraint with it. It has to refuse instead.
            using (var connectionInfo = new RawDatabase())
            {
                var queueName = GenerateQueueName.Create();
                var connectionString = connectionInfo.ConnectionString;
                var queueConnection = new QueueConnection(queueName, connectionString);
                var errorTable = $"{queueName}ErrorTracking";
                var indexName = $"IX_QueueIDExceptionType{errorTable}";
                var logProvider = LoggerShared.Create(queueName, GetType().Name);

                using (var container = new QueueCreationContainer<SqLiteMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                using (var creation = container.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection))
                {
                    creation.Options.EnableWalMode = false;
                    Assert.IsTrue(creation.CreateQueue().Success);

                    //somebody else's table, holding the name this script wants
                    Execute(connectionString, $"DROP INDEX {indexName}");
                    Execute(connectionString, "CREATE TABLE somebody_elses (a INTEGER, b INTEGER)");
                    Execute(connectionString, $"CREATE UNIQUE INDEX {indexName} ON somebody_elses (a, b)");
                }

                Quiesce();
                var error = Assert.Throws<SQLiteException>(
                    () => Execute(connectionString, UpgradeScript.Read("sqlite.sql", queueName)),
                    "the script went ahead and dropped an index that belongs to another table");

                Assert.Contains("dnwq_index_name_is_on_another_table", error.Message,
                    "the failure did not name the guard, so an operator cannot tell what to fix");

                //and it left the other table's constraint alone
                Assert.AreEqual(1, Scalar(connectionString,
                    $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = '{indexName}' AND tbl_name = 'somebody_elses'"),
                    "somebody else's index was dropped");
            }
        }

        private static bool UniqueIndexFound(QueueConnection queueConnection,
            Microsoft.Extensions.Logging.ILogger logProvider, string errorTable)
        {
            using (var container = new QueueContainer<SqLiteMessageQueueInit>(
                serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            using (var admin = container.CreateAdminContainer(queueConnection))
            {
                var query = admin.GetInstance<IQueryHandler<GetErrorTrackingUniqueIndexExistsQuery, bool>>();
                return query.Handle(new GetErrorTrackingUniqueIndexExistsQuery(errorTable));
            }
        }

        /// <summary>
        /// Releases the pooled connections - the test's version of the procedure's "stop everything
        /// using the file" step, which the journal mode change genuinely requires.
        /// </summary>
        private static void Quiesce()
        {
            SQLiteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private static string JournalMode(string connectionString)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "PRAGMA journal_mode";
                    return Convert.ToString(command.ExecuteScalar())?.ToLowerInvariant();
                }
            }
        }

        private static void Insert(string connectionString, string errorTable, long queueId, string exceptionType, int retries)
        {
            Execute(connectionString,
                $"INSERT INTO {errorTable} (QueueID, ExceptionType, RetryCount) VALUES ({queueId}, '{exceptionType}', {retries})");
        }

        private static int RowCount(string connectionString, string errorTable, long queueId, string exceptionType)
        {
            return Scalar(connectionString,
                $"SELECT COUNT(*) FROM {errorTable} WHERE QueueID = {queueId} AND ExceptionType = '{exceptionType}'");
        }

        private static int RetryCount(string connectionString, string errorTable, long queueId, string exceptionType)
        {
            return Scalar(connectionString,
                $"SELECT RetryCount FROM {errorTable} WHERE QueueID = {queueId} AND ExceptionType = '{exceptionType}'");
        }

        private static int Scalar(string connectionString, string sql)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = sql;
                    var value = command.ExecuteScalar();
                    return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
                }
            }
        }

        private static void Execute(string connectionString, string sql)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// A database file the library has not seen, and its clean-up.
        /// </summary>
        /// <remarks>
        /// The file is deliberately not created here. A file that already exists sends queue creation
        /// down its other branch, which applies the journal mode after the tables are made - and the
        /// database then comes out in WAL whatever the options say, so there would be no older database
        /// to upgrade.
        /// </remarks>
        private sealed class RawDatabase : IDisposable
        {
            private readonly string _fileName;

            public RawDatabase()
            {
                _fileName = Path.Combine(Path.GetTempPath(), GenerateQueueName.CreateFileName());
                ConnectionString = $"Data Source={_fileName};Version=3;";
            }

            public string ConnectionString { get; }

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
