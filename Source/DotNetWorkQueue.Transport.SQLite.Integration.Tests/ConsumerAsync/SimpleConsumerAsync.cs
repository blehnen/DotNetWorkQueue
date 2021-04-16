using System;
using System.Threading.Tasks;
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
    public class SimpleConsumerAsync
    {
        [Theory]
        [InlineData(100, 1, 400, 10, 5, 5, 1, true, false),
         InlineData(100, 1, 400, 10, 5, 5, 1, false, false),
         InlineData(10, 1, 400, 10, 5, 5, 1, false, true)]
        public async Task Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount,
            int queueSize,
            int messageType, bool inMemoryDb, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.
                        SimpleConsumerAsync();
                await consumer.Run<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(queueName,
                    connectionInfo.ConnectionString,
                    messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, enableChaos, x =>
                        Helpers.SetOptions(x,
                            false, true, false,
                            false, true, true, false),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount).ConfigureAwait(false);
            }
        }
    }
}
