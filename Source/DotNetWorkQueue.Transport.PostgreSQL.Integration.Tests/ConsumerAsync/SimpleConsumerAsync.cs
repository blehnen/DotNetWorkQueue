using System;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.ConsumerAsync
{
    [Collection("consumerasync")]
    public class SimpleConsumerAsync
    {
        [Theory]
        [InlineData(500, 1, 400, 10, 5, 5, false, 1, false),
         InlineData(500, 1, 400, 10, 5, 5, true, 1, false),
         InlineData(500, 0, 180, 10, 5, 0, false, 1, false),
         InlineData(500, 0, 180, 10, 5, 0, true, 1, false),
         InlineData(50, 0, 180, 10, 5, 0, false, 1, true),
         InlineData(50, 0, 180, 10, 5, 0, true, 1, true)]
        public async Task Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            bool useTransactions, int messageType, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.SimpleConsumerAsync();
            await consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(queueName,
                ConnectionInfo.ConnectionString,
                messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions, false,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount).ConfigureAwait(false);
        }
    }
}
