using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.Producer
{
    [Collection("Producer")]
    public class SimpleProducer
    {
        [Theory]
        [InlineData(1000, true, true, true, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, false, true, true, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, false, false, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, true, false, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, false, true, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, true, true, true, IntegrationConnectionInfo.ConnectionTypes.Direct),

         InlineData(10, true, true, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, false, true, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, false, false, false, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, true, false, false, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, false, true, false, IntegrationConnectionInfo.ConnectionTypes.Shared)]
        public void Run(
            int messageCount,
            bool interceptors,
            bool enableStatusTable,
            bool enableChaos,
            IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
                    new QueueCreationContainer<LiteDbMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, connectionInfo.ConnectionString);
                    ICreationScope scope = null;
                    var oCreation = queueCreator.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection);
                    try
                    {
                        oCreation.Options.EnableStatusTable = enableStatusTable;

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);
                        scope = oCreation.Scope;

                        var producer = new ProducerShared();
                        producer.RunTest<LiteDbMessageQueueInit, FakeMessage>(queueConnection, interceptors,
                            messageCount, logProvider,
                            Helpers.GenerateData,
                            Helpers.Verify, false, oCreation.Scope, enableChaos);
                    }
                    finally
                    {
                        oCreation?.RemoveQueue();
                        oCreation?.Dispose();
                        scope?.Dispose();
                    }
                }
            }
        }
    }
}
