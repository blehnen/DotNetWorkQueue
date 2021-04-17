using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.Producer
{
    [Collection("Producer")]
    public class MultiProducer
    {
        [Theory]
        [InlineData(100,  false, IntegrationConnectionInfo.ConnectionTypes.Memory),
        InlineData(10, true, IntegrationConnectionInfo.ConnectionTypes.Direct)]
        public void Run(int messageCount, bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var producer = new DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation.MultiProducer();
                producer.Run<LiteDbMessageQueueInit, FakeMessage, LiteDbMessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString),
                    messageCount, enableChaos, 10,x => { }, Helpers.GenerateData, Helpers.Verify, VerifyQueueData);
            }
        }

        private void VerifyQueueData(QueueConnection arg1, IBaseTransportOptions arg2, ICreationScope arg3, long arg4, long arg5, string arg6)
        {
            new VerifyQueueData(arg1, (LiteDbMessageQueueTransportOptions)arg2, arg3).Verify(arg4 * arg5, arg6);
        }
    }
}
