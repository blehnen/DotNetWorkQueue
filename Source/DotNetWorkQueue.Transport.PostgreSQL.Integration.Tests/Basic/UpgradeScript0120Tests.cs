using System;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Basic
{
    /// <summary>
    /// The upgrade script published for 0.12.0, run against a queue on the 0.11.0 schema.
    ///
    /// It runs the file that ships in docs/upgrade/0.12.0 rather than a copy of it, because a copy
    /// proves nothing about what an operator is handed - the two drift the moment either is edited
    /// alone, and the test keeps passing.
    ///
    /// The older queue is made by putting a new one back: the unique index is dropped, and the
    /// timestamptz columns are converted to naive timestamp in a session fixed to the writer's zone.
    /// That second step is not an approximation of what 0.11.0 did, it is the same conversion -
    /// Npgsql sent a UTC value, PostgreSQL moved it into the session zone, and the naive column kept
    /// the local time under a name ending in Utc.
    ///
    /// GitHub #321.
    /// </summary>
    [TestClass]
    [Retry(1)]
    public class UpgradeScript0120Tests
    {
        /// <summary>
        /// The zone the imaginary application wrote from. Fixed, and not the machine's, so the test
        /// means the same thing on every machine that runs it - and deliberately not UTC, because a
        /// conversion that did nothing would pass against UTC.
        /// </summary>
        private const string WriterZone = "America/Chicago";

        /// <summary>The instant a message was enqueued at, as the application meant it.</summary>
        private static readonly DateTime EnqueuedUtc = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);

        /// <summary>The same instant in the writer's zone, which is what 0.11.0 actually stored.</summary>
        private const string EnqueuedAsStoredByOldVersions = "2001-02-02 22:05:06";

        [TestMethod]
        public void TheScript_BringsAnOlderQueueUpToDate()
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = ConnectionInfo.ConnectionString;
            var queueConnection = new QueueConnection(queueName, connectionString);
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var errorTable = $"{queueName}ErrorTracking";
            var historyTable = $"{queueName}History";

            using (var queueCreator = new QueueCreationContainer<PostgreSqlMessageQueueInit>(
                serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var oCreation = queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
                try
                {
                    //History is off by default, and it is the table step 2 matters most on - it is the
                    //one that keeps values long enough for a shifted timestamp to be noticed
                    oCreation.Options.EnableHistory = true;
                    Assert.IsTrue(oCreation.CreateQueue().Success);

                    //--- put the queue back to how 0.11.0 left it ---
                    Execute(connectionString, $"DROP INDEX IX_QueueIDExceptionType{errorTable}");

                    Execute(connectionString,
                        $@"INSERT INTO {historyTable} (QueueID, Status, EnqueuedUtc, RetryCount)
                           VALUES ('a-message', 0, timestamptz '2001-02-03 04:05:06+00', 0)");

                    Execute(connectionString, $@"
                        SET TIME ZONE '{WriterZone}';
                        ALTER TABLE {historyTable} ALTER COLUMN EnqueuedUtc  TYPE timestamp;
                        ALTER TABLE {historyTable} ALTER COLUMN StartedUtc   TYPE timestamp;
                        ALTER TABLE {historyTable} ALTER COLUMN CompletedUtc TYPE timestamp;
                        ALTER TABLE {queueName}MetaData ALTER COLUMN QueuedDateTime TYPE timestamp;");

                    //the older schema really is in place: a naive column holding local time
                    Assert.AreEqual("timestamp without time zone", ColumnType(connectionString, historyTable, "enqueuedutc"),
                        "the column was not put back to the older type, so step 2 has nothing to prove");
                    Assert.AreEqual(EnqueuedAsStoredByOldVersions,
                        Text(connectionString, $"SELECT EnqueuedUtc::text FROM {historyTable}"),
                        "the stored value is not what 0.11.0 would have written");
                    Assert.IsFalse(UniqueIndexFound(queueConnection, logProvider, oCreation.Scope, errorTable),
                        "the unique index was present before the upgrade, so step 1 has nothing to prove");

                    //the duplicate rows the check-then-write race produced on 0.11.0
                    Insert(connectionString, errorTable, 1, "System.Exception", 2);
                    Insert(connectionString, errorTable, 1, "System.Exception", 3);
                    Insert(connectionString, errorTable, 2, "System.InvalidOperationException", 1);
                    Insert(connectionString, errorTable, 3, "System.Exception", 4);
                    Insert(connectionString, errorTable, 3, "System.Exception", 1);
                    Insert(connectionString, errorTable, 3, "System.Exception", 2);

                    //--- the upgrade, exactly as an operator runs it ---
                    Execute(connectionString, Script(queueName));

                    //--- step 1 ---
                    Assert.IsTrue(UniqueIndexFound(queueConnection, logProvider, oCreation.Scope, errorTable),
                        "the queue still reports no unique index, so it would keep using the racy count");

                    Assert.AreEqual(1, RowCount(connectionString, errorTable, 1, "System.Exception"));
                    Assert.AreEqual(1, RowCount(connectionString, errorTable, 3, "System.Exception"));

                    //summed, not discarded - each row counted attempts that really happened, and a
                    //message that loses them silently gets more retries than it is configured for
                    Assert.AreEqual(5, RetryCount(connectionString, errorTable, 1, "System.Exception"),
                        "the retry counts of the collapsed rows were not preserved");
                    Assert.AreEqual(7, RetryCount(connectionString, errorTable, 3, "System.Exception"),
                        "the retry counts of the collapsed rows were not preserved");
                    Assert.AreEqual(1, RetryCount(connectionString, errorTable, 2, "System.InvalidOperationException"),
                        "a row with no duplicates was changed by the collapse");

                    //--- step 2 ---
                    Assert.AreEqual("timestamp with time zone", ColumnType(connectionString, historyTable, "enqueuedutc"));
                    Assert.AreEqual("timestamp with time zone", ColumnType(connectionString, $"{queueName}MetaData", "queueddatetime"));

                    //the point of the whole step: history written before the upgrade now reads back as
                    //the instant the application meant, rather than as local time under a Utc name
                    Assert.AreEqual(EnqueuedUtc, ReadUtc(connectionString, $"SELECT EnqueuedUtc FROM {historyTable}"),
                        "the existing history did not come back as the instant it was written at");
                }
                finally
                {
                    oCreation.RemoveQueue();
                    oCreation.Dispose();
                }
            }
        }

        [TestMethod]
        public void TheScript_CanBeRunTwice()
        {
            //an operator who is unsure whether it has already been run has to be able to just run it.
            //Step 2 is the one that matters here: converting an already-converted column a second time
            //would shift every timestamp again, and no error would be raised.
            var queueName = GenerateQueueName.Create();
            var connectionString = ConnectionInfo.ConnectionString;
            var queueConnection = new QueueConnection(queueName, connectionString);
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var errorTable = $"{queueName}ErrorTracking";
            var historyTable = $"{queueName}History";

            using (var queueCreator = new QueueCreationContainer<PostgreSqlMessageQueueInit>(
                serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var oCreation = queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
                try
                {
                    //History is off by default, and it is the table step 2 matters most on - it is the
                    //one that keeps values long enough for a shifted timestamp to be noticed
                    oCreation.Options.EnableHistory = true;
                    Assert.IsTrue(oCreation.CreateQueue().Success);

                    Execute(connectionString, $"DROP INDEX IX_QueueIDExceptionType{errorTable}");
                    Execute(connectionString,
                        $@"INSERT INTO {historyTable} (QueueID, Status, EnqueuedUtc, RetryCount)
                           VALUES ('a-message', 0, timestamptz '2001-02-03 04:05:06+00', 0)");
                    Execute(connectionString, $@"
                        SET TIME ZONE '{WriterZone}';
                        ALTER TABLE {historyTable} ALTER COLUMN EnqueuedUtc TYPE timestamp;");

                    Insert(connectionString, errorTable, 1, "System.Exception", 2);
                    Insert(connectionString, errorTable, 1, "System.Exception", 3);

                    var script = Script(queueName);
                    Execute(connectionString, script);
                    Execute(connectionString, script);

                    Assert.AreEqual(1, RowCount(connectionString, errorTable, 1, "System.Exception"));
                    Assert.AreEqual(5, RetryCount(connectionString, errorTable, 1, "System.Exception"),
                        "running the script twice changed the retry count");
                    Assert.AreEqual(EnqueuedUtc, ReadUtc(connectionString, $"SELECT EnqueuedUtc FROM {historyTable}"),
                        "running the script twice shifted the timestamps a second time");
                }
                finally
                {
                    oCreation.RemoveQueue();
                    oCreation.Dispose();
                }
            }
        }

        /// <summary>
        /// The shipped script, with the two edits its header tells an operator to make.
        /// </summary>
        private static string Script(string queueName)
        {
            return UpgradeScript.Read("postgresql.sql", queueName,
                new Dictionary<string, string>
                {
                    //the zone the application wrote from, which is the whole correctness of step 2
                    { "SET LOCAL TIME ZONE 'UTC';", $"SET LOCAL TIME ZONE '{WriterZone}';" }
                });
        }

        private static bool UniqueIndexFound(QueueConnection queueConnection,
            Microsoft.Extensions.Logging.ILogger logProvider, ICreationScope scope, string errorTable)
        {
            using (var container = new QueueContainer<PostgreSqlMessageQueueInit>(serviceRegister =>
            {
                serviceRegister.Register(() => logProvider, LifeStyles.Singleton);
                serviceRegister.RegisterNonScopedSingleton(scope);
            }))
            using (var admin = container.CreateAdminContainer(queueConnection))
            {
                //the shipped statement, run here rather than a copy of it. The library no longer
                //exposes a query for this - the error count write has no fallback to choose any more
                //(GitHub #308) - but schema version 1 still reads it, so this has to stay honest about
                //what that reads. Asserting against a copy would pass with a broken one in the library.
                var commandCache = admin.GetInstance<CommandStringCache>();
                using (var connection = new NpgsqlConnection(queueConnection.Connection))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText =
                            commandCache.GetCommand(CommandStringTypes.GetErrorTrackingUniqueIndexExists);
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = "@Table";
                        parameter.Value = errorTable;
                        command.Parameters.Add(parameter);
                        using (var reader = command.ExecuteReader())
                        {
                            return reader.Read();
                        }
                    }
                }
            }
        }

        private static string ColumnType(string connectionString, string table, string column)
        {
            return Text(connectionString,
                $@"SELECT data_type FROM information_schema.columns
                   WHERE table_name = lower('{table}') AND column_name = lower('{column}')");
        }

        private static DateTime ReadUtc(string connectionString, string sql)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = sql;
                    var value = (DateTime)command.ExecuteScalar();
                    return value.ToUniversalTime();
                }
            }
        }

        private static string Text(string connectionString, string sql)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = sql;
                    return Convert.ToString(command.ExecuteScalar());
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
            using (var conn = new NpgsqlConnection(connectionString))
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
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
