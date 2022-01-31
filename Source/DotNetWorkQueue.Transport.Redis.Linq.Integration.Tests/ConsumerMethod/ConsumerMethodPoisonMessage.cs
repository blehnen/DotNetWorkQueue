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
    public class ConsumerMethodPoisonMessage
    {
        [Theory]
#if NETFULL
        [InlineData(1, 20, 1, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic)]
#else
        [InlineData(1, 20, 1, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled)]
#endif
        public void Run(int messageCount, int timeOut,
            int workerCount, ConnectionInfoTypes type, LinqMethodTypes linqMethodTypes)
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation.
                    ConsumerMethodPoisonMessage();

            consumer.Run<RedisQueueInit, RedisQueueCreation>(new QueueConnection(queueName, connectionString),
                messageCount, timeOut, workerCount, linqMethodTypes, false, x => { },
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
