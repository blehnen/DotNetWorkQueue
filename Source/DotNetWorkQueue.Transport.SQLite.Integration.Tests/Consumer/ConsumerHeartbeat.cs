using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Consumer
{
    [Collection("Consumer")]
    public class ConsumerHeartbeat
    {
        [Theory]
        [InlineData(7, 45, 180, 3, true, false),
        InlineData(7, 45, 180, 3, false, false),
        InlineData(7, 45, 280, 3, true, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, bool inMemoryDb, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.ConsumerHeartbeat();
                consumer.Run<SqLiteMessageQueueInit, FakeMessage, SqLiteMessageQueueCreation>(new QueueConnection(queueName, connectionInfo.ConnectionString),
                    messageCount, runtime, timeOut, workerCount, enableChaos, x => Helpers.SetOptions(x,
                        true, true, false,
                        false, true, true, false),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
            }
        }
    }
}
