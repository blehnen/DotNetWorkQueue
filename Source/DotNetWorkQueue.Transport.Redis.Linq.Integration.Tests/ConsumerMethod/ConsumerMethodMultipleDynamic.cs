#if NETFULL
using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.ConsumerMethod
{
    [Collection("Redis")]
    public class ConsumerMethodMultipleDynamic
    {
        [Theory]
        [InlineData(100, 0, 140, 5, ConnectionInfoTypes.Linux),
        InlineData(100, 0, 240, 25, ConnectionInfoTypes.Windows)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, ConnectionInfoTypes type)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (
                var queueCreator =
                    new QueueCreationContainer<RedisQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {
                    var id = Guid.NewGuid();
                    var producer = new ProducerMethodMultipleDynamicShared();
                    producer.RunTestDynamic<RedisQueueInit>(queueName,
                        connectionString, false, messageCount, logProvider, Helpers.GenerateData,
                        Helpers.Verify, false, id, GenerateMethod.CreateMultipleDynamic, runtime, null);

                    var consumer = new ConsumerMethodShared();
                    consumer.RunConsumer<RedisQueueInit>(queueName, connectionString, false, logProvider,
                        runtime, messageCount,
                        workerCount, timeOut, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), id, "second(*%3)");

                    using (var count = new VerifyQueueRecordCount(queueName, connectionString))
                    {
                        count.Verify(0, false, -1);
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
#endif
