using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.ConsumerMethodAsync
{
    [Collection("ConsumerAsync")]
    public class ConsumerMethodAsyncErrorTable
    {
        [Theory]
#if NETFULL
         [InlineData(1, 30, 1, 1, 0, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(1, 30, 1, 1, 0, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled)]
#else
        [InlineData(1, 30, 1, 1, 0, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled)]
#endif
        public void Run(int messageCount, int timeOut, int workerCount, int readerCount, int queueSize, ConnectionInfoTypes type, LinqMethodTypes linqMethodTypes)
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync.Implementation.
                    ConsumerMethodAsyncErrorTable();

            consumer.Run<RedisQueueInit, RedisQueueCreation>(new QueueConnection(queueName, connectionString),
                messageCount, timeOut, workerCount, readerCount, queueSize, linqMethodTypes, false, x => { },
                Helpers.GenerateData, Helpers.Verify, VerifyQueueCount, ValidateErrorCounts);
        }

        private void ValidateErrorCounts(QueueConnection queueConnection, int arg3, ICreationScope arg4)
        {
            using (var error = new VerifyErrorCounts(queueConnection.Queue, queueConnection.Connection))
            {
                error.Verify(arg3, 2);
            }
        }

        private void VerifyQueueCount(QueueConnection queueConnection, IBaseTransportOptions arg3, ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            //noop
        }

        private void ValidateErrorCounts(string queueName, int messageCount, string connectionString)
        {
            using (var error = new VerifyErrorCounts(queueName, connectionString))
            {
                error.Verify(messageCount, 2);
            }
        }
    }
}
