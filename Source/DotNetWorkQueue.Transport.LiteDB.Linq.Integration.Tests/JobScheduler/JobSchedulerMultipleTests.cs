using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.JobScheduler
{
    [Collection("Consumer")]
    public class JobSchedulerMultipleTests
    {
        [Theory]
        [InlineData(2)]
        public void Run(
            int producerCount)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(IntegrationConnectionInfo.ConnectionTypes.Direct))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.JobScheduler.Implementation.JobSchedulerMultipleTests();

                consumer.Run<LiteDbMessageQueueInit, LiteDbJobQueueCreation, LiteDbMessageQueueCreation>(queueName,
                    connectionInfo.ConnectionString, producerCount);
            }
        }
    }
}
