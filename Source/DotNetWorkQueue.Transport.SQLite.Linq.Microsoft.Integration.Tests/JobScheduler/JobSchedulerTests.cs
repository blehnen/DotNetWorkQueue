using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using DotNetWorkQueue.Transport.SQLite.Microsoft.Basic;
using DotNetWorkQueue.Transport.SQLite.Microsoft.Integration.Tests;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Microsoft.Integration.Tests.JobScheduler
{
    [Collection("JobScheduler")]
    public class JobSchedulerTests
    {
        [Theory]
        [InlineData(false),
        InlineData(true)]
        public void Run(
            bool inMemoryDb)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                using (var queueCreator =
                    new QueueCreationContainer<SqLiteMessageQueueInit>())
                {
                    var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, connectionInfo.ConnectionString);
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection)
                    )
                    {
                        using (var queueContainer = new QueueContainer<SqLiteMessageQueueInit>(x => { }))
                        {
                            try
                            {
                                var tests = new JobSchedulerTestsShared();
                                tests.RunEnqueueTestCompiled<SqLiteMessageQueueInit, SqliteJobQueueCreation>(
                                    queueConnection, true,
                                    Helpers.Verify, Helpers.SetError,
                                    queueContainer.CreateTimeSync(connectionInfo.ConnectionString),
                                    oCreation.Scope, LoggerShared.Create(queueName, GetType().Name));
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
