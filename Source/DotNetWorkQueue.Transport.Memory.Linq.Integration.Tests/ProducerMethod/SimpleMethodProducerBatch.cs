using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ProducerMethod
{
    [Collection("Memory")]
    public class SimpleMethodProducerBatch
    {
        [Theory]
#if NETFULL
        [InlineData(1000, LinqMethodTypes.Dynamic),
         InlineData(1000, LinqMethodTypes.Compiled)]
#else
        [InlineData(1000, LinqMethodTypes.Compiled)]
#endif
        public void Run(
            int messageCount,
            LinqMethodTypes linqMethodTypes)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
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

                            var producer = new ProducerMethodShared();
                            var id = Guid.NewGuid();
                            if (linqMethodTypes == LinqMethodTypes.Compiled)
                            {
                               producer.RunTestCompiled<MemoryMessageQueueInit>(queueName,
                               connectionInfo.ConnectionString, true, messageCount, logProvider,
                               Helpers.GenerateData,
                               Helpers.Verify, true, id, GenerateMethod.CreateCompiled, 0, oCreation.Scope);
                            }
#if NETFULL
                            else
                            {
                               producer.RunTestDynamic<MemoryMessageQueueInit>(queueName,
                               connectionInfo.ConnectionString, true, messageCount, logProvider,
                               Helpers.GenerateData,
                               Helpers.Verify, true, id, GenerateMethod.CreateDynamic, 0, oCreation.Scope);
                            }
#endif
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
}
