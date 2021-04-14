using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Consumer
{
    [Collection("Consumer")]
    public class SimpleConsumer
    {
        [Theory]
        [InlineData(1000, 0, 60, 5, true, false),
        InlineData(10, 45, 120, 5, false, false),
        InlineData(10, 0, 60, 5, true, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, bool inMemoryDb, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var consumer = new DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.SimpleConsumer();
                consumer.Run<SqLiteMessageQueueInit, FakeMessage, SqLiteMessageQueueCreation>(queueName,
                    connectionInfo.ConnectionString,
                    messageCount, runtime, timeOut, workerCount, enableChaos, x => Helpers.SetOptions(x,
                        true, true, false,
                        false, true, true, false),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
            }
        }
    }
}
