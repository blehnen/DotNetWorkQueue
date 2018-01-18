using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.ProducerMethod
{
    [Collection("Redis")]
    public class SimpleMethodProducerAsync
    {
        [Theory]
        [InlineData(100, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
#if NETFULL
        InlineData(100, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
        InlineData(100, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
        InlineData(100, false, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
        InlineData(100, true, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
        InlineData(100, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
        InlineData(100, true, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
        InlineData(100, false, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
#endif
        InlineData(100, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
        InlineData(100, true, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
        InlineData(100, false, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
        InlineData(100, true, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
        InlineData(100, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
        InlineData(100, true, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
        InlineData(100, false, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled)]
        public async void Run(
           int messageCount,
           bool interceptors,
           bool batchSending,
           ConnectionInfoTypes type,
           LinqMethodTypes linqMethodTypes)
        {

            var id = Guid.NewGuid();
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var producer = new ProducerMethodAsyncShared();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (
                var queueCreator =
                    new QueueCreationContainer<RedisQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {
                    await producer.RunTestAsync<RedisQueueInit>(queueName,
                        connectionString, interceptors, messageCount, logProvider, Helpers.GenerateData,
                        Helpers.Verify, batchSending, 0, id, linqMethodTypes, null).ConfigureAwait(false);
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<RedisQueueCreation>(queueName,
                                connectionString)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }
    }
}
