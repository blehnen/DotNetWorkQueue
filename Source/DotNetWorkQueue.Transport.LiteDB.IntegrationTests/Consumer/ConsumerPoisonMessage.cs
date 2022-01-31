using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.Consumer
{
    [Collection("Consumer")]
    public class ConsumerPoisonMessage
    {
        [Theory]
        [InlineData(2, 60, 1, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(10, 60, 5, false, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(2, 60, 1, true, IntegrationConnectionInfo.ConnectionTypes.Shared)]
        public void Run(int messageCount, int timeOut, int workerCount, bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.ConsumerPoisonMessage();
                consumer.Run<LiteDbMessageQueueInit, FakeMessage, LiteDbMessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString),
                    messageCount, timeOut, workerCount, enableChaos, x => Helpers.SetOptions(x, true, false, true),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount, ValidateErrorCounts);
            }
        }

        private void ValidateErrorCounts(QueueConnection queueConnection, long arg3, ICreationScope arg4)
        {
            //poison messages are moved to the error queue right away
            //they don't update the tracking table, so specify 0 for the error count.
            //They still update the error table itself
            new VerifyErrorCounts(queueConnection.Queue, queueConnection.Connection, arg4).Verify(arg3, 0);
        }
    }
}
