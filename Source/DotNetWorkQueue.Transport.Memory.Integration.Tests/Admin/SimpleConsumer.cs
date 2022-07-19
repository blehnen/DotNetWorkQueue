using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.Admin
{
    [Collection("consumeradmin")]
    public class SimpleConsumer
    {
        [Theory]
        [InlineData(5, 15, 60, 5),
        InlineData(25, 10, 200, 10),
        InlineData(10, 15, 180, 7)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var producer = new DotNetWorkQueue.IntegrationTests.Shared.Admin.Implementation.SimpleConsumerAdmin();
                producer.Run<MemoryMessageQueueInit, FakeMessage, MessageQueueCreation>(new QueueConnection(queueName,
                    connectionInfo.ConnectionString),
                    messageCount, runtime, timeOut, workerCount, false, x => { },
                    Helpers.GenerateData, Helpers.Verify, VerifyQueueCount);
            }
        }

        private void VerifyQueueCount(QueueConnection queueConnection, IBaseTransportOptions arg3, ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            new VerifyQueueRecordCount().Verify(arg4, arg5, true);
        }
    }
}
