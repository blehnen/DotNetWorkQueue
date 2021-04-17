using System;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.ProducerMethod
{
    [Collection("Producer")]
    public class SimpleMethodProducerAsync
    {
        [Theory]
        [InlineData(100, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
#if NETFULL
        InlineData(100, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
        InlineData(100, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
        InlineData(100, false, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
#endif
        InlineData(100, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
        InlineData(100, true, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
        InlineData(100, false, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled)]
        public async Task Run(
           int messageCount,
           bool interceptors,
           bool batchSending,
           ConnectionInfoTypes type,
           LinqMethodTypes linqMethodTypes)
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation.SimpleMethodProducerAsync();

            await consumer.Run<RedisQueueInit, RedisQueueCreation>(new QueueConnection(queueName, connectionString),
                messageCount, linqMethodTypes, interceptors, false, batchSending, x => { },
                Helpers.GenerateData, Helpers.Verify).ConfigureAwait(false);
        }
    }
}
