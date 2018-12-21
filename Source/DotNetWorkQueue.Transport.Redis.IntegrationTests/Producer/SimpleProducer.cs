using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Producer
{
    [Collection("Redis")]
    public class SimpleProducer
    {
        [Theory]
        [InlineData(100, true, false, false, false, ConnectionInfoTypes.Linux),
         InlineData(100, false, false, false, false, ConnectionInfoTypes.Linux),
         InlineData(500, true, false, false, false, ConnectionInfoTypes.Linux),
         InlineData(500, false, false, false, false, ConnectionInfoTypes.Linux),
         InlineData(100, true, true, false, false, ConnectionInfoTypes.Linux),
         InlineData(100, false, true, false, false, ConnectionInfoTypes.Linux),
         InlineData(100, true, false, true, false, ConnectionInfoTypes.Linux),
         InlineData(100, false, false, false, true, ConnectionInfoTypes.Linux),
         InlineData(100, true, false, true, true, ConnectionInfoTypes.Linux),
         InlineData(100, true, true, true, false, ConnectionInfoTypes.Linux),
         InlineData(100, false, true, true, true, ConnectionInfoTypes.Linux)]
        public void Run(
            int messageCount,
            bool interceptors,
            bool batchSending,
            bool enableDelay,
            bool enableExpiration,
            ConnectionInfoTypes type)
        {

            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var producer = new ProducerShared();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (
                var queueCreator =
                    new QueueCreationContainer<RedisQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {
                    if (enableExpiration && enableDelay)
                    {
                       producer.RunTest<RedisQueueInit, FakeMessage>(queueName,
                       connectionString, interceptors, messageCount, logProvider, Helpers.GenerateDelayExpiredData,
                       Helpers.Verify, batchSending, null);
                    }
                    else if (enableDelay)
                    {
                       producer.RunTest<RedisQueueInit, FakeMessage>(queueName,
                       connectionString, interceptors, messageCount, logProvider, Helpers.GenerateDelayData,
                       Helpers.Verify, batchSending, null);
                    }
                    else if (enableExpiration)
                    {
                        producer.RunTest<RedisQueueInit, FakeMessage>(queueName,
                       connectionString, interceptors, messageCount, logProvider, Helpers.GenerateExpiredData,
                       Helpers.Verify, batchSending, null);
                    }
                    else
                    {
                        producer.RunTest<RedisQueueInit, FakeMessage>(queueName,
                        connectionString, interceptors, messageCount, logProvider, Helpers.GenerateData,
                        Helpers.Verify, batchSending, null);
                    }
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
