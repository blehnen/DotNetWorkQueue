#if NETFULL
using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;
#endif

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ConsumerMethod
{
#if NETFULL
    public class ConsumerMethodMultipleDynamic
    {
        [Theory]
        [InlineData(1000, 0, 120, 5)]
        public void Run(int messageCount, int runtime,
            int timeOut, int workerCount)
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
                    try
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<MessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
                            )
                        {
                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            var producer = new ProducerMethodMultipleDynamicShared();
                            var id = Guid.NewGuid();
                            producer.RunTestDynamic<MemoryMessageQueueInit>(queueName,
                                connectionInfo.ConnectionString, false, messageCount, logProvider,
                                Helpers.GenerateData,
                                Helpers.Verify, false, id, GenerateMethod.CreateMultipleDynamic, runtime, oCreation.Scope);

                            var consumer = new ConsumerMethodShared();
                            consumer.RunConsumer<MemoryMessageQueueInit>(queueName, connectionInfo.ConnectionString,
                                false,
                                logProvider,
                                runtime, messageCount,
                                workerCount, timeOut,
                                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id, "second(*%10)");

                            new VerifyQueueRecordCount()
                                .Verify(oCreation.Scope, 0, true);
                        }
                    }
                    finally
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<MessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
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
}
