using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Producer
{
    [Collection("Producer")]
    public class SimpleProducerAsync
    {
        [Theory]
        [InlineData(100, true, false, ConnectionInfoTypes.Linux),
        InlineData(100, false, false, ConnectionInfoTypes.Linux),
        InlineData(250, true, false, ConnectionInfoTypes.Linux),
        InlineData(200, false, false, ConnectionInfoTypes.Linux),
        InlineData(100, true, true, ConnectionInfoTypes.Linux),
        InlineData(100, false, true, ConnectionInfoTypes.Linux)]
        public async void Run(
           int messageCount,
           bool interceptors,
           bool batchSending,
           ConnectionInfoTypes type)
        {

            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var producer = new ProducerAsyncShared();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (
                var queueCreator =
                    new QueueCreationContainer<RedisQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var queueConnection = new QueueConnection(queueName, connectionString);
                try
                {
                    await producer.RunTestAsync<RedisQueueInit, FakeMessage>(queueConnection, interceptors, messageCount, logProvider, Helpers.GenerateData,
                        Helpers.Verify, batchSending, null, false).ConfigureAwait(false);
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<RedisQueueCreation>(queueConnection)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }
    }
}
