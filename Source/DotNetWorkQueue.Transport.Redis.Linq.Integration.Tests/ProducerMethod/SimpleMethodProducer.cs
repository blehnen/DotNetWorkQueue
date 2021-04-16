using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.ProducerMethod
{
    [Collection("Producer")]
    public class SimpleMethodProducer
    {
        [Theory]
        [InlineData(100, true, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
#if NETFULL
         InlineData(100, true, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(100, false, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(500, true, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(500, false, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(100, true, true, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(100, false, true, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(100, true, false, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(100, false, false, false, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(100, true, false, true, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(100, true, true, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(100, false, true, true, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
#endif
         InlineData(100, false, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(500, true, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(500, false, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(100, true, true, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(100, false, true, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(100, true, false, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(100, false, false, false, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(100, true, false, true, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(100, true, true, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(100, false, true, true, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled)]
        public void Run(
            int messageCount,
            bool interceptors,
            bool batchSending,
            bool enableDelay,
            bool enableExpiration,
            ConnectionInfoTypes type,
            LinqMethodTypes linqMethodTypes)
        {

            var queueName = GenerateQueueName.Create();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation.SimpleMethodProducer();

            if (enableExpiration && enableDelay)
            {
                consumer.Run<RedisQueueInit, RedisQueueCreation>(queueName,
                    connectionString,
                    messageCount, linqMethodTypes, interceptors, false, batchSending, creation => { }, Helpers.GenerateDelayExpiredData, Verify);
            }

            else if (enableDelay)
            {
                consumer.Run<RedisQueueInit, RedisQueueCreation>(queueName,
                    connectionString,
                    messageCount, linqMethodTypes, interceptors, false, batchSending, creation => { }, Helpers.GenerateDelayData, Verify);
            }

            else if (enableExpiration)
            {
                consumer.Run<RedisQueueInit, RedisQueueCreation>(queueName,
                    connectionString,
                    messageCount, linqMethodTypes, interceptors, false, batchSending, creation => { }, Helpers.GenerateExpiredData, Verify);
            }
            else
            {
                consumer.Run<RedisQueueInit, RedisQueueCreation>(queueName,
                    connectionString,
                    messageCount, linqMethodTypes, interceptors, false, batchSending, creation => { }, Helpers.GenerateData, Verify);
            }
        }

        private void Verify(QueueConnection arg1, QueueProducerConfiguration arg2, long arg3, ICreationScope arg4)
        {
            //noop
        }
    }
}
