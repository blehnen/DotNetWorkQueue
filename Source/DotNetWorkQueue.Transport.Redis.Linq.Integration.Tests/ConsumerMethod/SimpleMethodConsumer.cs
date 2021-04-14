using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.ConsumerMethod
{
    [Collection("Consumer")]
    public class SimpleMethodConsumer
    {
        [Theory]
#if NETFULL
        [InlineData(100, 0, 240, 5, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
        InlineData(50, 5, 200, 10, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic)]
#else
        [InlineData(10, 5, 180, 7, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled)]
#endif
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, ConnectionInfoTypes type, LinqMethodTypes linqMethodTypes)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (
                var queueCreator =
                    new QueueCreationContainer<RedisQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var queueConnection = new QueueConnection(queueName, connectionString);
                try
                {
                    var id = Guid.NewGuid();
                    var producer = new ProducerMethodShared();
                    if (linqMethodTypes == LinqMethodTypes.Compiled)
                    {
                        producer.RunTestCompiled<RedisQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, id, GenerateMethod.CreateCompiled, runtime, null, false);
                    }
#if NETFULL
                    else
                    {
                        producer.RunTestDynamic<RedisQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                           Helpers.Verify, false, id, GenerateMethod.CreateDynamic, runtime, null, false);
                    }
#endif
                    var consumer = new ConsumerMethodShared();
                    consumer.RunConsumer<RedisQueueInit>(queueConnection, false, logProvider,
                        runtime, messageCount,
                        workerCount, timeOut, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), id, "second(*%3)", false, new CreationScopeNoOp());

                    using (var count = new VerifyQueueRecordCount(queueName, connectionString))
                    {
                        count.Verify(0, false, -1);
                    }
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
