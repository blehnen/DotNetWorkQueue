using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.Producer
{
    [Collection("producer")]
    public class MultiProducer
    {
        [Fact]
        public void Run()
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var producer = new DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation.MultiProducer();
                producer.Run<MemoryMessageQueueInit, FakeMessage, MessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString),
                    100, false,10, x => { }, Helpers.GenerateData, Verify, VerifyQueueData);
            }
        }

        private void Verify(QueueConnection arg1, QueueProducerConfiguration arg2, long arg3, ICreationScope arg4)
        {
            Helpers.Verify(arg1, arg2, arg3 * 10, arg4);
        }

        private void VerifyQueueData(QueueConnection arg1, IBaseTransportOptions arg2, ICreationScope arg3, long arg4, long arg5, string arg6)
        {
            //we don't verify this in memory transport
        }
    }
}
