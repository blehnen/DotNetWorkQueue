using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Consumer
{
    [Collection("Consumer")]
    public class ConsumerCancelWork
    {
        [Theory]
        [InlineData(7, 5, 90, 3, ConnectionInfoTypes.Linux, false),
        InlineData(7, 5, 90, 3, ConnectionInfoTypes.Linux, false)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, ConnectionInfoTypes type, bool route)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (var queueCreator =
                new QueueCreationContainer<RedisQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {
                    var producer = new ProducerShared();
                    if (route)
                    {
                        producer.RunTest<RedisQueueInit, FakeMessage>(queueName,
                            connectionString, false, messageCount, logProvider, Helpers.GenerateRouteData,
                            Helpers.Verify, false, null, false);
                    }
                    else
                    {
                        producer.RunTest<RedisQueueInit, FakeMessage>(queueName,
                            connectionString, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, null, false);
                    }

                    var defaultRoute = route ? Helpers.DefaultRoute : null;

                    var consumer = new ConsumerCancelWorkShared<RedisQueueInit, FakeMessage>();
                    consumer.RunConsumer(queueName, connectionString, false, logProvider,
                        runtime, messageCount,
                        workerCount, timeOut, x => { }, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)", defaultRoute, false);

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
