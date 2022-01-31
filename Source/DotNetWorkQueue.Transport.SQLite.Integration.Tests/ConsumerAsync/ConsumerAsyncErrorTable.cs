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
    public class ConsumerAsyncErrorTable
    {
        [Theory]
        [InlineData(1, 60, 1, 1, 0, true, false),
        InlineData(25, 200, 20, 1, 5, false, false),
        InlineData(5, 60, 20, 1, 5, false, true)]
        public void Run(int messageCount, int timeOut, int workerCount,
            int readerCount, int queueSize, bool inMemoryDb, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.ConsumerAsyncErrorTable();
                consumer.Run<SqLiteMessageQueueInit, FakeMessage, SqLiteMessageQueueCreation>(new QueueConnection(queueName, connectionInfo.ConnectionString),
                    messageCount, timeOut, workerCount, readerCount, queueSize, enableChaos, x => Helpers.SetOptions(x,
                        false, true, false,
                        false, true, true, false),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount, ValidateErrorCounts);
            }
        }

        private void ValidateErrorCounts(QueueConnection queueConnection, int arg3, ICreationScope arg4)
        {
            new VerifyErrorCounts(queueConnection.Queue, queueConnection.Connection).Verify(arg3, 2);
        }
    }
}
