using System;
using System.Threading;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Consumer
{
    [Collection("Consumer")]
    public class ConsumerExpiredMessage
    {
        [Theory]
        [InlineData(100, 0, 20, 5, ConnectionInfoTypes.Linux, false),
        InlineData(500, 0, 120, 5, ConnectionInfoTypes.Linux, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, ConnectionInfoTypes type, bool route)
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
                    if (route)
                    {
                        var producer = new ProducerShared();
                        producer.RunTest<RedisQueueInit, FakeMessage>(queueName,
                            connectionString, false, messageCount, logProvider, Helpers.GenerateExpiredDataWithRoute,
                            Helpers.Verify, false, null, false);
                    }
                    else
                    {
                        var producer = new ProducerShared();
                        producer.RunTest<RedisQueueInit, FakeMessage>(queueName,
                            connectionString, false, messageCount, logProvider, Helpers.GenerateExpiredData,
                            Helpers.Verify, false, null, false);
                    }

                    Thread.Sleep(2000);

                    var defaultRoute = route ? Helpers.DefaultRoute : null;
                    var consumer = new ConsumerExpiredMessageShared<FakeMessage>();
                    consumer.RunConsumer<RedisQueueInit>(queueName, connectionString, false,
                        logProvider,
                        runtime, messageCount,
                        workerCount, timeOut, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)", defaultRoute, false);

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
