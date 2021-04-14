using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.Consumer
{
    [Collection("Consumer")]
    public class SimpleConsumer
    {
        [Theory]
        [InlineData(10, 0, 60, 2, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
        InlineData(2, 45, 120, 2, false, IntegrationConnectionInfo.ConnectionTypes.Memory),
        InlineData(20, 0, 90, 2, true, IntegrationConnectionInfo.ConnectionTypes.Shared)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount,  bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var consumer = new DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.SimpleConsumer();
                consumer.Run<LiteDbMessageQueueInit, FakeMessage, LiteDbMessageQueueCreation>(queueName,
                    connectionInfo.ConnectionString,
                    messageCount, runtime, timeOut, workerCount, enableChaos, x => { },
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
            }
        }
    }
}
