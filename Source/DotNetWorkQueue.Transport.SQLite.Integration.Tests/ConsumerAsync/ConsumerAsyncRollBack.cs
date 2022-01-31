using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.ConsumerAsync
{
    [Collection("Consumer")]
    public class ConsumerAsyncRollBack
    {
        [Theory]
        [InlineData(100, 1, 400, 5, 5, 5, true, false),
         InlineData(10, 45, 260, 5, 1, 2, false, false),
         InlineData(10, 0, 400, 5, 5, 5, true, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount,
            int readerCount, int queueSize, bool inMemoryDb, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.
                        ConsumerAsyncRollBack();
                consumer.Run<SqLiteMessageQueueInit, FakeMessage, SqLiteMessageQueueCreation>(new QueueConnection(queueName, connectionInfo.ConnectionString),
                    messageCount, runtime, timeOut, workerCount, readerCount, queueSize, enableChaos, x => Helpers.SetOptions(x,
                        true, true, false,
                        false, true, true, false),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
            }
        }
    }
}
