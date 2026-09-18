using System;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.Data.SqlClient;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Basic
{
    /// <summary>
    /// Counting errors against a real database.
    ///
    /// Every queue carries a unique index on (QueueID, ExceptionType) and counts with one atomic
    /// statement: new queues are created with it, and an older one gains it at schema version 1, which
    /// a producer or consumer refuses to start without. The check-then-write fallback these once
    /// covered on both schemas is gone (GitHub #308), so what is left is the atomic path and the two
    /// ways a write can arrive out of order against it - a replay of the same total, and a stale lower
    /// one.
    ///
    /// The assertion that the index is detected stays, and it runs the library's own statement rather
    /// than a copy: a name-based check passed here once while the catalog had lower-cased the name.
    /// </summary>
    [TestClass]
    [Retry(1)]
    public class SetErrorCountTests
    {
        [TestMethod]
        public void CountsErrors_AndIgnoresAReplayedOrStaleTotal()
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = ConnectionInfo.ConnectionString;
            var queueConnection = new QueueConnection(queueName, connectionString);
            var logProvider = LoggerShared.Create(queueName, GetType().Name);

            using (var queueCreator = new QueueCreationContainer<SqlServerMessageQueueInit>(
                serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var oCreation = queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection);
                try
                {
                    var result = oCreation.CreateQueue();
                    Assert.IsTrue(result.Success, result.ErrorMessage);

                    var errorTable = $"{queueName}ErrorTracking";

                    Assert.IsTrue(UniqueIndexFound(queueConnection, logProvider, oCreation.Scope, errorTable),
                        "a new queue did not report the unique index, so it would keep using the racy count");

                    //the atomic statement, on the schema that supports it
                    CountTwice(queueConnection, logProvider, oCreation.Scope, 1);
                    Assert.AreEqual(2, RetryCount(connectionString, errorTable, 1));

                    //and writing that same total again changes nothing. The write is wrapped in a retry
                    //policy, so a transient fault raised after the server had already committed it replays
                    //the statement - which used to count one real failure twice and cost the message an
                    //attempt it never used (GitHub #350).
                    CountOnce(queueConnection, logProvider, oCreation.Scope, 1, 2);
                    Assert.AreEqual(2, RetryCount(connectionString, errorTable, 1),
                        "replaying the same total counted a second failure");

                    //and a stale total cannot undo it. A worker whose claim lapsed can still be holding a
                    //count it read before another worker advanced the row, and that write may land afterwards;
                    //letting it lower the count would hand the message attempts it had already used.
                    CountOnce(queueConnection, logProvider, oCreation.Scope, 1, 1);
                    Assert.AreEqual(2, RetryCount(connectionString, errorTable, 1),
                        "a stale lower total overwrote a higher one");
                }
                finally
                {
                    oCreation.RemoveQueue();
                    oCreation.Dispose();
                }
            }
        }

        [TestMethod]
        public void CountsConcurrentFirstFailures_AsOneRow()
        {
            //the race the unique index and the single statement exist for: several workers failing the
            //same message at the same moment, none of which has an error row to update yet.
            //Thirty-two rather than a handful because SQLite serialises writers - at eight the older
            //path came through unscathed there, and a test that cannot fail against the behaviour it
            //replaces is not saying anything
            const int failures = 32;
            var queueName = GenerateQueueName.Create();
            var connectionString = ConnectionInfo.ConnectionString;
            var queueConnection = new QueueConnection(queueName, connectionString);
            var logProvider = LoggerShared.Create(queueName, GetType().Name);

            using (var queueCreator = new QueueCreationContainer<SqlServerMessageQueueInit>(
                serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var oCreation = queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection);
                try
                {
                    var result = oCreation.CreateQueue();
                    Assert.IsTrue(result.Success, result.ErrorMessage);

                    var errorTable = $"{queueName}ErrorTracking";
                    Assert.IsTrue(UniqueIndexFound(queueConnection, logProvider, oCreation.Scope, errorTable),
                        "a new queue did not report the unique index, so this would not be testing the atomic path");

                    CountConcurrently(queueConnection, logProvider, oCreation.Scope, 1, failures);

                    Assert.AreEqual(1, RowCount(connectionString, errorTable, 1),
                        "concurrent first failures wrote more than one row for the same message and exception type");
                    //One, not `failures`. Since #350 the count is a total the caller supplies rather
                    //than an increment the statement applies, and every racer here read the same count
                    //before writing - so they all agree on the same total. Losing the other failures is
                    //the deliberate cost of making the write idempotent: a replayed retry no longer
                    //counts one failure twice, at the price of concurrent failures counting as one.
                    //An extra attempt still ends at the error queue; a missing one does not.
                    Assert.AreEqual(1, RetryCount(connectionString, errorTable, 1),
                        "the count is not the total the callers supplied");
                }
                finally
                {
                    oCreation.RemoveQueue();
                    oCreation.Dispose();
                }
            }
        }

        private static void CountConcurrently(QueueConnection queueConnection,
            Microsoft.Extensions.Logging.ILogger logProvider, ICreationScope scope, long queueId, int failures)
        {
            using (var container = Container(logProvider, scope))
            using (var admin = container.CreateAdminContainer(queueConnection))
            {
                var handler = admin.GetInstance<ICommandHandler<SetErrorCountCommand<long>>>();
                //resolved once and started together - the point is that they overlap
                using (var start = new ManualResetEventSlim(false))
                {
                    var running = new Task[failures];
                    for (var i = 0; i < failures; i++)
                    {
                        running[i] = Task.Factory.StartNew(() =>
                        {
                            start.Wait();
                            //every racer read the same count, because none of them has a row yet
                            handler.Handle(new SetErrorCountCommand<long>("System.Exception", queueId, 1));
                        }, TaskCreationOptions.LongRunning);
                    }
                    start.Set();
                    Task.WaitAll(running);
                }
            }
        }

        private static int RowCount(string connectionString, string errorTable, long queueId)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = $"SELECT COUNT(*) FROM {errorTable} WHERE QueueID = @id";
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@id";
                    parameter.Value = queueId;
                    command.Parameters.Add(parameter);
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        [TestMethod]
        public void DoesNotMistakeAnIncludedColumnForAKeyColumn()
        {
            //a unique index on QueueID alone that merely stores ExceptionType alongside it. It has the
            //two column names and it is unique, but it guarantees one row per message rather than one
            //per message and exception type - the single statement would break on the second exception
            var queueName = GenerateQueueName.Create();
            var connectionString = ConnectionInfo.ConnectionString;
            var queueConnection = new QueueConnection(queueName, connectionString);
            var logProvider = LoggerShared.Create(queueName, GetType().Name);

            using (var queueCreator = new QueueCreationContainer<SqlServerMessageQueueInit>(
                serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var oCreation = queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection);
                try
                {
                    var result = oCreation.CreateQueue();
                    Assert.IsTrue(result.Success, result.ErrorMessage);

                    var errorTable = $"{queueName}ErrorTracking";
                    Execute(connectionString, $"DROP INDEX IX_QueueIDExceptionType ON {errorTable}");
                    Execute(connectionString, $"CREATE UNIQUE INDEX IX_Included ON {errorTable} (QueueID) INCLUDE (ExceptionType)");

                    Assert.IsFalse(UniqueIndexFound(queueConnection, logProvider, oCreation.Scope, errorTable),
                        "an included column was counted as part of the unique key");
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
            using (var container = Container(logProvider, scope))
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

        /// <summary>
        /// Writes one error count, the way a replayed retry would.
        /// </summary>
        private static void CountOnce(QueueConnection queueConnection,
            Microsoft.Extensions.Logging.ILogger logProvider, ICreationScope scope, long queueId, int retryCount)
        {
            using (var container = Container(logProvider, scope))
            using (var admin = container.CreateAdminContainer(queueConnection))
            {
                var handler = admin.GetInstance<ICommandHandler<SetErrorCountCommand<long>>>();
                handler.Handle(new SetErrorCountCommand<long>("System.Exception", queueId, retryCount));
            }
        }

        private static void CountTwice(QueueConnection queueConnection,
            Microsoft.Extensions.Logging.ILogger logProvider, ICreationScope scope, long queueId)
        {
            //a fresh container each time, so the cached answer about the schema is taken again
            using (var container = Container(logProvider, scope))
            using (var admin = container.CreateAdminContainer(queueConnection))
            {
                var handler = admin.GetInstance<ICommandHandler<SetErrorCountCommand<long>>>();
                //the count is a total, and the real caller reads it before each write - so a first
                //failure records one and the second records two
                handler.Handle(new SetErrorCountCommand<long>("System.Exception", queueId, 1));
                handler.Handle(new SetErrorCountCommand<long>("System.Exception", queueId, 2));
            }
        }

        private static QueueContainer<SqlServerMessageQueueInit> Container(
            Microsoft.Extensions.Logging.ILogger logProvider, ICreationScope scope)
        {
            return new QueueContainer<SqlServerMessageQueueInit>(serviceRegister =>
            {
                serviceRegister.Register(() => logProvider, LifeStyles.Singleton);
                serviceRegister.RegisterNonScopedSingleton(scope);
            });
        }

        private static int RetryCount(string connectionString, string errorTable, long queueId)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = $"SELECT RetryCount FROM {errorTable} WHERE QueueID = @id";
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@id";
                    parameter.Value = queueId;
                    command.Parameters.Add(parameter);
                    using (var reader = command.ExecuteReader())
                    {
                        return reader.Read() ? reader.GetInt32(0) : 0;
                    }
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
