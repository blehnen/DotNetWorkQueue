using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.JobScheduler
{
    [Collection("Consumer")]
    public class JobSchedulerTests
    {
        [Theory]
        [InlineData(false),
        InlineData(true)]
        public void Run(
            bool dynamic)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(IntegrationConnectionInfo.ConnectionTypes.Direct))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.JobScheduler.Implementation.JobSchedulerTests();

                consumer.Run<LiteDbMessageQueueInit, LiteDbJobQueueCreation, LiteDbMessageQueueCreation>(new QueueConnection(queueName,
                    connectionInfo.ConnectionString), false, dynamic, Helpers.Verify, Helpers.SetError);
            }
        }
    }
}
