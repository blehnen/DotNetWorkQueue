using System.Data.SQLite;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Basic
{
    /// <summary>
    /// Counting errors against a real database, with and without the unique index.
    ///
    /// A new queue gets a unique index on (QueueID, ExceptionType) and a single atomic statement. A queue
    /// created before that index existed does not have it, and the library does not upgrade schemas - so
    /// the older check-then-write path has to keep working against the older table. Dropping the index
    /// here is how an old queue is simulated, and it is the only way to exercise that path now that new
    /// queues take the other branch.
    /// </summary>
    [TestClass]
    public class SetErrorCountAgainstBothSchemasTests
    {
        [TestMethod]
        public void CountsErrors_OnANewQueueAndOnOneWithoutTheIndex()
        {
            using (var connectionInfo = new IntegrationConnectionInfo(false))
            {
                var queueName = GenerateQueueName.Create();
                var queueConnection = new QueueConnection(queueName, connectionInfo.ConnectionString);
                var logProvider = LoggerShared.Create(queueName, GetType().Name);

                using (var queueCreator = new QueueCreationContainer<SqLiteMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    var oCreation = queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection);
                    try
                    {
                        var result = oCreation.CreateQueue();
                        Assert.IsTrue(result.Success, result.ErrorMessage);

                        var errorTable = $"{queueName}ErrorTracking";
                        var indexName = $"IX_QueueIDExceptionType{errorTable}";

                        Assert.IsTrue(IndexExists(connectionInfo.ConnectionString, indexName),
                            "a newly created queue should carry the unique index");

                        //the atomic statement, on the schema that supports it
                        CountTwice(queueConnection, logProvider, oCreation.Scope, 1);
                        Assert.AreEqual(2, RetryCount(connectionInfo.ConnectionString, errorTable, 1));

                        //now an older queue: same table, no index
                        Execute(connectionInfo.ConnectionString, $"DROP INDEX {indexName}");
                        Assert.IsFalse(IndexExists(connectionInfo.ConnectionString, indexName));

                        //a different message id, so this counts from zero on the fallback path
                        CountTwice(queueConnection, logProvider, oCreation.Scope, 2);
                        Assert.AreEqual(2, RetryCount(connectionInfo.ConnectionString, errorTable, 2),
                            "the fallback stopped counting errors on a queue without the index");
                    }
                    finally
                    {
                        oCreation.RemoveQueue();
                        oCreation.Dispose();
                    }
                }
            }
        }

        private static void CountTwice(QueueConnection queueConnection, Microsoft.Extensions.Logging.ILogger logProvider,
            ICreationScope scope, long queueId)
        {
            //a fresh container each time, so the cached answer about the schema is taken again
            using (var container = new QueueContainer<SqLiteMessageQueueInit>(serviceRegister =>
            {
                serviceRegister.Register(() => logProvider, LifeStyles.Singleton);
                serviceRegister.RegisterNonScopedSingleton(scope);
            }))
            {
                using (var admin = container.CreateAdminContainer(queueConnection))
                {
                    var handler = admin.GetInstance<ICommandHandler<SetErrorCountCommand<long>>>();
                    handler.Handle(new SetErrorCountCommand<long>("System.Exception", queueId));
                    handler.Handle(new SetErrorCountCommand<long>("System.Exception", queueId));
                }
            }
        }

        private static bool IndexExists(string connectionString, string indexName)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='index' AND name=@name";
                    command.Parameters.AddWithValue("@name", indexName);
                    using (var reader = command.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        private static int RetryCount(string connectionString, string errorTable, long queueId)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = $"SELECT RetryCount FROM {errorTable} WHERE QueueID = @id";
                    command.Parameters.AddWithValue("@id", queueId);
                    using (var reader = command.ExecuteReader())
                    {
                        return reader.Read() ? reader.GetInt32(0) : 0;
                    }
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
    }
}
