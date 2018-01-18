using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.JobScheduler
{
    [Collection("SQLite")]
    public class JobSchedulerTests
    {
        [Theory]
        [InlineData(false, false),
        InlineData(true, false),
        InlineData(false, true)]
        public void Run(
            bool dynamic,
            bool inMemoryDb)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                using (var queueCreator =
                    new QueueCreationContainer<SqLiteMessageQueueInit>())
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueName,
                                connectionInfo.ConnectionString)
                    )
                    {
                        using (var queueContainer = new QueueContainer<SqLiteMessageQueueInit>(x => { }))
                        {
                            try
                            {
                                var tests = new JobSchedulerTestsShared();
                                if (!dynamic)
                                {
                                    tests.RunEnqueueTestCompiled<SqLiteMessageQueueInit, SqliteJobQueueCreation>(
                                        queueName,
                                        connectionInfo.ConnectionString, true,
                                        Helpers.Verify, Helpers.SetError,
                                        queueContainer.CreateTimeSync(connectionInfo.ConnectionString),
                                            oCreation.Scope, LoggerShared.Create(queueName, GetType().Name));
                                }
                                else
                                {
                                    tests.RunEnqueueTestDynamic<SqLiteMessageQueueInit, SqliteJobQueueCreation>(
                                        queueName,
                                        connectionInfo.ConnectionString, true,
                                        Helpers.Verify, Helpers.SetError,
                                        queueContainer.CreateTimeSync(connectionInfo.ConnectionString),
                                            oCreation.Scope, LoggerShared.Create(queueName, GetType().Name));
                                }
                            }
                            finally
                            {
                                oCreation.RemoveQueue();
                            }
                        }
                    }
                }
            }
        }
    }
}
