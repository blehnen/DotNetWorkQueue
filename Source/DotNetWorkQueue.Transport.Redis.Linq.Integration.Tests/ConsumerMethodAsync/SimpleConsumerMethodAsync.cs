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
    public class SimpleConsumerMethodAsync
    {
        [Theory]
#if NETFULL
         [InlineData(100, 0, 180, 10, 5, 0, 1, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic)]
#else
         [InlineData(10, 5, 180, 7, 1, 1, 1, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled)]
#endif
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            int messageType, ConnectionInfoTypes type, LinqMethodTypes linqMethodTypes)
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync.Implementation.
                    SimpleMethodConsumerAsync();

            consumer.Run<RedisQueueInit, RedisQueueCreation>(queueName,
                connectionString,
                messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, linqMethodTypes, false, x => { },
                Helpers.GenerateData, Helpers.Verify, VerifyQueueCount);
        }

        private void VerifyQueueCount(string arg1, string arg2, IBaseTransportOptions arg3, ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            using (var count = new VerifyQueueRecordCount(arg1, arg2))
            {
                count.Verify(0, false, -1);
            }
        }
    }
}
