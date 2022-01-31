using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.ConsumerAsync
{
    [Collection("ConsumerAsync")]
    public class ConsumerAsyncPoisonMessage
    {
        [Theory]
        [InlineData(1, 60, 1, 1, 0, ConnectionInfoTypes.Linux),
        InlineData(10, 60, 5, 1, 0, ConnectionInfoTypes.Linux)]
        public void Run(int messageCount, int timeOut, int workerCount,
            int readerCount, int queueSize, ConnectionInfoTypes type)
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.ConsumerAsyncPoisonMessage();

            consumer.Run<RedisQueueInit, FakeMessage, RedisQueueCreation>(new QueueConnection(queueName, connectionString),
                messageCount, timeOut, workerCount, readerCount, queueSize, false, x => { },
                Helpers.GenerateData, Helpers.Verify, VerifyQueueCount, ValidateErrorCounts);
        }

        private void ValidateErrorCounts(QueueConnection queueConnection, int arg3, ICreationScope arg4)
        {
            //poison messages are moved to the error queue right away
            //they don't update the tracking table, so specify 0 for the error count.
            //They still update the error table itself
            using (var error = new VerifyErrorCounts(queueConnection.Queue, queueConnection.Connection))
            {
                error.Verify(arg3, 0);
            }
        }

        private void VerifyQueueCount(QueueConnection queueConnection, IBaseTransportOptions arg3, ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            using (
                var count = new VerifyQueueRecordCount(queueConnection.Queue, queueConnection.Connection))
            {
                count.Verify(arg5, true, 2);
            }
        }
    }
}
