using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ConsumerMethod
{
    [Collection("consumer")]
    public class SimpleMethodConsumer
    {
        [Theory]
#if NETFULL
        [InlineData(100, 0, 30, 5, LinqMethodTypes.Dynamic),
        InlineData(10, 15, 60, 7, LinqMethodTypes.Compiled)]
#else
        [InlineData(10, 15, 60, 7, LinqMethodTypes.Compiled)]
#endif
        public void Run(int messageCount, int runtime, 
            int timeOut, int workerCount, LinqMethodTypes linqMethodTypes)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (
                    var queueCreator =
                        new QueueCreationContainer<MemoryMessageQueueInit>(
                            serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    var queueConnection = new QueueConnection(queueName, connectionInfo.ConnectionString);
                    try
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<MessageQueueCreation>(queueConnection)
                            )
                        {
                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            var producer = new ProducerMethodShared();
                            var id = Guid.NewGuid();
                            if (linqMethodTypes == LinqMethodTypes.Compiled)
                            {
                                producer.RunTestCompiled<MemoryMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, id, GenerateMethod.CreateCompiled, runtime, oCreation.Scope, false);
                            }
#if NETFULL
                            else
                            {
                                producer.RunTestDynamic<MemoryMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, id, GenerateMethod.CreateDynamic, runtime, oCreation.Scope, false);
                            }
#endif
                            var consumer = new ConsumerMethodShared();
                            consumer.RunConsumer<MemoryMessageQueueInit>(queueConnection,
                                false,
                                logProvider,
                                runtime, messageCount,
                                workerCount, timeOut,
                                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id, "second(*%10)", false);

                            new VerifyQueueRecordCount().Verify(oCreation.Scope, 0, true);
                        }
                    }
                    finally
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<MessageQueueCreation>(queueConnection)
                            )
                        {
                            oCreation.RemoveQueue();
                        }
                    }
                }
            }
        }
    }
}
