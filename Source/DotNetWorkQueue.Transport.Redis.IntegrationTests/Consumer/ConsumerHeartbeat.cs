using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Consumer
{
    [Collection("Consumer")]
    public class ConsumerHeartbeat
    {
        [Theory]
        [InlineData(7, 15, 90, 3, ConnectionInfoTypes.Linux)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, ConnectionInfoTypes type)
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            var consumer = new DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.ConsumerHeartbeat();

            consumer.Run<RedisQueueInit, FakeMessage, RedisQueueCreation>(new QueueConnection(queueName, connectionString),
                messageCount, runtime, timeOut, workerCount, false, x => { },
                Helpers.GenerateData, Verify, VerifyQueueCount);
        }
        private void VerifyQueueCount(QueueConnection queueConnection, IBaseTransportOptions arg3, ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            using (var count = new VerifyQueueRecordCount(queueConnection.Queue, queueConnection.Connection))
            {
                count.Verify(0, false, -1);
            }
        }

        private void Verify(QueueConnection arg1, QueueProducerConfiguration arg2, long arg3, ICreationScope arg4)
        {
            //only verify count in redis
        }
    }
}
