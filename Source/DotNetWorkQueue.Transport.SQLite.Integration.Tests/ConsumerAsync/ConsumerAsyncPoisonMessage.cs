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
    public class ConsumerAsyncPoisonMessage
    {
        [Theory]
        [InlineData(1, 60, 2, 2, 0, true, false),
         InlineData(25, 60, 10, 2, 2, false, false),
         InlineData(5, 60, 10, 2, 2, false, true)]
        public void Run(int messageCount, int timeOut, int workerCount, int readerCount, int queueSize, bool inMemoryDb, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.
                        ConsumerAsyncPoisonMessage();
                consumer.Run<SqLiteMessageQueueInit, FakeMessage, SqLiteMessageQueueCreation>(new QueueConnection(queueName, connectionInfo.ConnectionString),
                    messageCount, timeOut, workerCount, readerCount, queueSize, enableChaos, x => Helpers.SetOptions(x,
                        false, false, false,
                        false, true, false, false),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount, ValidateErrorCounts);
            }
        }

        private void ValidateErrorCounts(QueueConnection queueConnection, int arg3, ICreationScope arg4)
        {
            //poison messages are moved to the error queue right away
            //they don't update the tracking table, so specify 0 for the error count.
            //They still update the error table itself
            new VerifyErrorCounts(queueConnection.Queue, queueConnection.Connection).Verify(arg3, 0);
        }
    }
}
