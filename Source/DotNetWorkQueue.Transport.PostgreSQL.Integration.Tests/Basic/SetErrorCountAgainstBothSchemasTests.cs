using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Basic
{
    /// <summary>
    /// Counting errors against a real database, with and without the unique index.
    ///
    /// A new queue gets a unique index on (QueueID, ExceptionType) and counts with a single atomic
    /// statement. A queue created before that index existed does not have one, and the library does not
    /// upgrade schemas, so the older check-then-write has to keep working against the older table.
    /// Dropping the index is how an old queue is simulated, and it is the only way to reach that path
    /// now that new queues take the other branch.
    ///
    /// The assertions on detection are the ones that matter. Both paths count correctly when nothing is
    /// racing, so a test that only counted would pass even while the queue silently never used the
    /// atomic statement - which is what a name-based check did, against a catalog that lower-cases it.
    /// </summary>
    [TestClass]
    public class SetErrorCountAgainstBothSchemasTests
    {
        [TestMethod]
        public void CountsErrors_OnANewQueueAndOnOneWithoutTheIndex()
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = ConnectionInfo.ConnectionString;
            var queueConnection = new QueueConnection(queueName, connectionString);
            var logProvider = LoggerShared.Create(queueName, GetType().Name);

            using (var queueCreator = new QueueCreationContainer<PostgreSqlMessageQueueInit>(
                serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var oCreation = queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
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

                    //now an older queue: same table, no index
                    Execute(connectionString, $"DROP INDEX IX_QueueIDExceptionType{errorTable}");
                    Assert.IsFalse(UniqueIndexFound(queueConnection, logProvider, oCreation.Scope, errorTable),
                        "the index was still reported after it had been dropped");

                    //a different message id, so this counts from zero on the fallback path
                    CountTwice(queueConnection, logProvider, oCreation.Scope, 2);
                    Assert.AreEqual(2, RetryCount(connectionString, errorTable, 2),
                        "the fallback stopped counting errors on a queue without the index");
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

            using (var queueCreator = new QueueCreationContainer<PostgreSqlMessageQueueInit>(
                serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var oCreation = queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
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
                    Assert.AreEqual(failures, RetryCount(connectionString, errorTable, 1),
                        "a concurrent failure was not counted");
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
                            handler.Handle(new SetErrorCountCommand<long>("System.Exception", queueId));
                        }, TaskCreationOptions.LongRunning);
                    }
                    start.Set();
                    Task.WaitAll(running);
                }
            }
        }

        private static int RowCount(string connectionString, string errorTable, long queueId)
        {
            using (var conn = new NpgsqlConnection(connectionString))
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

            using (var queueCreator = new QueueCreationContainer<PostgreSqlMessageQueueInit>(
                serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var oCreation = queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
                try
                {
                    var result = oCreation.CreateQueue();
                    Assert.IsTrue(result.Success, result.ErrorMessage);

                    var errorTable = $"{queueName}ErrorTracking";
                    Execute(connectionString, $"DROP INDEX IX_QueueIDExceptionType{errorTable}");
                    Execute(connectionString, $"CREATE UNIQUE INDEX IX_Included{errorTable} ON {errorTable} (QueueID) INCLUDE (ExceptionType)");

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
                var query = admin.GetInstance<IQueryHandler<GetErrorTrackingUniqueIndexExistsQuery, bool>>();
                return query.Handle(new GetErrorTrackingUniqueIndexExistsQuery(errorTable));
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
                handler.Handle(new SetErrorCountCommand<long>("System.Exception", queueId));
                handler.Handle(new SetErrorCountCommand<long>("System.Exception", queueId));
            }
        }

        private static QueueContainer<PostgreSqlMessageQueueInit> Container(
            Microsoft.Extensions.Logging.ILogger logProvider, ICreationScope scope)
        {
            return new QueueContainer<PostgreSqlMessageQueueInit>(serviceRegister =>
            {
                serviceRegister.Register(() => logProvider, LifeStyles.Singleton);
                serviceRegister.RegisterNonScopedSingleton(scope);
            });
        }

        private static int RetryCount(string connectionString, string errorTable, long queueId)
        {
            using (var conn = new NpgsqlConnection(connectionString))
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
