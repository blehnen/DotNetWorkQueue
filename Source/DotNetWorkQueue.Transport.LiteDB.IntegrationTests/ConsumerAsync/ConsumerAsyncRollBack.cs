using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.ConsumerAsync
{
    [Collection("ConsumerAsync")]
    public class ConsumerAsyncRollBack
    {
        [Theory]
        [InlineData(50, 1, 400, 5, 1, 5, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(5, 45, 260, 5, 1, 2, false, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, 0, 400, 5, 1, 5, true, IntegrationConnectionInfo.ConnectionTypes.Shared)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount,
            int readerCount, int queueSize, bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.
                        ConsumerAsyncRollBack();
                consumer.Run<LiteDbMessageQueueInit, FakeMessage, LiteDbMessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString),
                    messageCount, runtime, timeOut, workerCount, readerCount, queueSize, enableChaos, x => Helpers.SetOptions(x, true, false, true),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
            }
        }
    }
}
