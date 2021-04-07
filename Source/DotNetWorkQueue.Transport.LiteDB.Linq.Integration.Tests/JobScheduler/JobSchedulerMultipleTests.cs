using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.JobScheduler
{
    [Collection("JobScheduler")]
    public class JobSchedulerMultipleTests
    {
        [Theory]
        [InlineData(2)]
        public void Run(
            int producerCount)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                using (var queueContainer = new QueueContainer<LiteDbMessageQueueInit>(x => {
                }))
                {
                    var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, connectionInfo.ConnectionString);
                    try
                    {
                        var tests = new JobSchedulerTestsShared();
                        tests.RunTestMultipleProducers<LiteDbMessageQueueInit, LiteDbJobQueueCreation>(queueConnection, true, producerCount, queueContainer.CreateTimeSync(connectionInfo.ConnectionString), LoggerShared.Create(queueName, GetType().Name));
                    }
                    finally
                    {

                        using (var queueCreator =
                            new QueueCreationContainer<LiteDbMessageQueueInit>())
                        {
                            using (
                                var oCreation =
                                    queueCreator.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection)
                                )
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
