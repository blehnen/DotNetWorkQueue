#if NETFULL
using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.ConsumerMethod
{
    [Collection("Consumer")]
    public class ConsumerMethodMultipleDynamic
    {
        [Theory]
        [InlineData(100, 0, 140, 5, ConnectionInfoTypes.Linux)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, ConnectionInfoTypes type)
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation.
                    ConsumerMethodMultipleDynamic();

            consumer.Run<RedisQueueInit, RedisQueueCreation>(new QueueConnection(queueName,
                    connectionString),
                messageCount, runtime, timeOut, workerCount, false, x => { },
                Helpers.GenerateData, Helpers.Verify, VerifyQueueCount);
        }

        private void VerifyQueueCount(QueueConnection queueConnection, IBaseTransportOptions arg3, ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            using (var count = new VerifyQueueRecordCount(queueConnection.Queue, queueConnection.Connection))
            {
                count.Verify(0, false, -1);
            }
        }
    }
}
#endif
