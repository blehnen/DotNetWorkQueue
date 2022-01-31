using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.Producer
{
    [Collection("producer")]
    public class SimpleProducer
    {
        [Theory]
        [InlineData(1000, true),
         InlineData(1000, false)]
        public void Run(
            int messageCount,
            bool interceptors)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var producer = new DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation.SimpleProducer();
                producer.Run<MemoryMessageQueueInit, FakeMessage, MessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString),
                    messageCount, interceptors, false, false, x => { },
                    Helpers.GenerateData, Helpers.Verify);
            }
        }
    }
}
