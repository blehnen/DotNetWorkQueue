using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.Producer
{
    [Collection("Producer")]
    public class SimpleProducer
    {
        [Theory]
        [InlineData(1000, true, true, true, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, false, true, true, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, false, false, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, true, false, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, false, true, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, true, true, true, IntegrationConnectionInfo.ConnectionTypes.Direct),

         InlineData(10, true, true, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, false, true, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, false, false, false, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, true, false, false, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, false, true, false, IntegrationConnectionInfo.ConnectionTypes.Shared)]
        public void Run(
            int messageCount,
            bool interceptors,
            bool enableStatusTable,
            bool enableChaos,
            IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var producer = new DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation.SimpleProducer();
                producer.Run<LiteDbMessageQueueInit, FakeMessage, LiteDbMessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString),
                    messageCount, interceptors, enableChaos, false, x => x.Options.EnableStatusTable = enableStatusTable,
                    Helpers.GenerateData, Helpers.Verify);
            }
        }
    }
}
