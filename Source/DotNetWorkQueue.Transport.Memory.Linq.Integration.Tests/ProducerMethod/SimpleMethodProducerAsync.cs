using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ProducerMethod
{
    [Collection("producer")]
    public class SimpleMethodProducerAsync
    {
        [Theory]
#if NETFULL
        [InlineData(1000, LinqMethodTypes.Dynamic),
         InlineData(1000, LinqMethodTypes.Compiled)]
#else
        [InlineData(1000, LinqMethodTypes.Compiled)]
#endif
        public async void Run(
            int messageCount,
            LinqMethodTypes linqMethodTypes)
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

                            var producer = new ProducerMethodAsyncShared();
                            var id = Guid.NewGuid();
                            await producer.RunTestAsync<MemoryMessageQueueInit>(queueConnection, true, messageCount, logProvider,
                                Helpers.GenerateData,
                                Helpers.Verify, false, 0, id, linqMethodTypes, oCreation.Scope, false).ConfigureAwait(false);
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
