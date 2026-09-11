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
