using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.Producer
{
    [Collection("Producer")]
    public class SimpleProducerAsync
    {
        [Theory]
        [InlineData(1000, true, true, true, false, false, true, false,  false),
         InlineData(100, false, true, true, false, false, true, false, false),
         InlineData(100, false, false, false, false, false, false, false,  false),
         InlineData(100, true, false, false, false, false, false, false,   false),
         InlineData(100, false, false, false, false, false, false, true,   false),
         InlineData(100, false, false, false, false, false, true, true,  false),
         InlineData(100, false, true, false, true, true, false, true,   false),
         InlineData(100, false, true, true, true, true, true, true, false),
         InlineData(100, true, true, true, false, false, true, false, false),

         InlineData(10, true, true, true, false, false, true, false,  true),
         InlineData(10, false, true, true, false, false, true, false,   true),
         InlineData(10, false, false, false, false, false, false, false,  true),
         InlineData(10, true, false, false, false, false, false, false,  true),
         InlineData(10, false, false, false, false, false, false, true,  true),
         InlineData(10, false, false, false, false, false, true, true,  true),
         InlineData(10, false, true, false, true, true, false, true, true),
         InlineData(10, false, true, true, true, true, true, true,   true)]
        public async void Run(
            int messageCount,
            bool interceptors,
            bool enableDelayedProcessing,
            bool enableHeartBeat,
            bool enableMessageExpiration,
            bool enablePriority,
            bool enableStatus,
            bool enableStatusTable,
            bool enableChaos)
        {

            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (
                    var queueCreator =
                        new QueueCreationContainer<LiteDbMessageQueueInit>(
                            serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, connectionInfo.ConnectionString);
                    try
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection)
                            )
                        {
                            oCreation.Options.EnableStatusTable = enableStatusTable;

                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            var producer = new ProducerAsyncShared();
                            await producer.RunTestAsync<LiteDbMessageQueueInit, FakeMessage>(queueConnection, interceptors, messageCount, logProvider,
                                Helpers.GenerateData,
                                Helpers.Verify, false, oCreation.Scope, enableChaos).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection)
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
