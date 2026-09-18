using System;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Basic
{
    /// <summary>
    /// The upgrade script published for 0.12.0, run against a queue on the 0.11.0 schema.
    ///
    /// It runs the file that ships in docs/upgrade/0.12.0 rather than a copy of it, because a copy
    /// proves nothing about what an operator is handed - the two drift the moment either is edited
    /// alone, and the test keeps passing.
    ///
    /// SQL Server has only the one step. Its timestamp columns are datetime and were always stored as
    /// written, so #311's correction was PostgreSQL-only; dropping the unique index is the whole of
    /// putting a queue back to the older shape.
    ///
    /// GitHub #321.
    /// </summary>
    [TestClass]
    [Retry(1)]
    public class UpgradeScript0120Tests
    {
        [TestMethod]
        public void TheScript_BringsAnOlderQueueUpToDate()
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = ConnectionInfo.ConnectionString;
            var queueConnection = new QueueConnection(queueName, connectionString);
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var errorTable = $"{queueName}ErrorTracking";

            using (var queueCreator = new QueueCreationContainer<SqlServerMessageQueueInit>(
                serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var oCreation = queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection);
                try
                {
                    Assert.IsTrue(oCreation.CreateQueue().Success);

                    //--- put the queue back to how 0.11.0 left it ---
                    Execute(connectionString, $"DROP INDEX IX_QueueIDExceptionType ON {errorTable}");

                    Assert.IsFalse(UniqueIndexFound(queueConnection, logProvider, oCreation.Scope, errorTable),
                        "the unique index was present before the upgrade, so the script has nothing to prove");

                    //the duplicate rows the check-then-write race produced on 0.11.0: two workers
                    //failing the same message both inserted, and the count then read low from either
                    Insert(connectionString, errorTable, 1, "System.Exception", 2);
                    Insert(connectionString, errorTable, 1, "System.Exception", 3);
                    Insert(connectionString, errorTable, 2, "System.InvalidOperationException", 1);
                    Insert(connectionString, errorTable, 3, "System.Exception", 4);
                    Insert(connectionString, errorTable, 3, "System.Exception", 1);
                    Insert(connectionString, errorTable, 3, "System.Exception", 2);

                    //--- the upgrade, exactly as an operator runs it ---
                    Execute(connectionString, UpgradeScript.Read("sqlserver.sql", queueName));

                    Assert.IsTrue(UniqueIndexFound(queueConnection, logProvider, oCreation.Scope, errorTable),
                        "the queue still reports no unique index, so it would keep using the racy count");

                    Assert.AreEqual(1, RowCount(connectionString, errorTable, 1, "System.Exception"));
                    Assert.AreEqual(1, RowCount(connectionString, errorTable, 3, "System.Exception"));

                    //the largest of the group, not their sum. RetryCount is an absolute total, and
                    //the queue's own write already takes the greater of the stored and supplied value,
                    //so summing would roughly double it and retire the message early (GitHub #374)
                    Assert.AreEqual(3, RetryCount(connectionString, errorTable, 1, "System.Exception"),
                        "the surviving row does not carry the largest count of its group");
                    Assert.AreEqual(4, RetryCount(connectionString, errorTable, 3, "System.Exception"),
                        "the surviving row does not carry the largest count of its group");

                    //a row that was never duplicated has to come through untouched
                    Assert.AreEqual(1, RetryCount(connectionString, errorTable, 2, "System.InvalidOperationException"),
                        "a row with no duplicates was changed by the collapse");
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
            //an operator who is unsure whether it has already been run has to be able to just run it
            var queueName = GenerateQueueName.Create();
            var connectionString = ConnectionInfo.ConnectionString;
            var queueConnection = new QueueConnection(queueName, connectionString);
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var errorTable = $"{queueName}ErrorTracking";

            using (var queueCreator = new QueueCreationContainer<SqlServerMessageQueueInit>(
                serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var oCreation = queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection);
                try
                {
                    Assert.IsTrue(oCreation.CreateQueue().Success);

                    Execute(connectionString, $"DROP INDEX IX_QueueIDExceptionType ON {errorTable}");
                    Insert(connectionString, errorTable, 1, "System.Exception", 2);
                    Insert(connectionString, errorTable, 1, "System.Exception", 3);

                    var script = UpgradeScript.Read("sqlserver.sql", queueName);
                    Execute(connectionString, script);
                    Execute(connectionString, script);

                    Assert.AreEqual(1, RowCount(connectionString, errorTable, 1, "System.Exception"));
                    //the second run must not sum an already-summed value again
                    Assert.AreEqual(3, RetryCount(connectionString, errorTable, 1, "System.Exception"),
                        "running the script twice changed the retry count");
                }
                finally
                {
                    oCreation.RemoveQueue();
                    oCreation.Dispose();
                }
            }
        }

        private static bool UniqueIndexFound(QueueConnection queueConnection,
            Microsoft.Extensions.Logging.ILogger logProvider, ICreationScope scope, string errorTable)
        {
            using (var container = new QueueContainer<SqlServerMessageQueueInit>(serviceRegister =>
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
                using (var connection = new SqlConnection(queueConnection.Connection))
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
            using (var conn = new SqlConnection(connectionString))
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
            using (var conn = new SqlConnection(connectionString))
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
