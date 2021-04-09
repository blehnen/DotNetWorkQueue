﻿using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.Producer
{
    [Collection("Producer")]
    public class SimpleProducerBatch
    {
        [Theory]
        [InlineData(1000, true, true, true),
         InlineData(100, false, true, true),
         InlineData(100, false, false, false),
         InlineData(100, true, false, false),
         InlineData(100, false, true, false),
         InlineData(100, true, true, true),

         InlineData(10, true, true, true),
         InlineData(10, false, true, true),
         InlineData(10, false, false, false),
         InlineData(10, true, false, false),
         InlineData(10, false, true, false)]
        public void Run(
            int messageCount,
            bool interceptors,
            bool enableStatusTable,
            bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
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

                            var producer = new ProducerShared();
                            producer.RunTest<LiteDbMessageQueueInit, FakeMessage>(queueConnection, interceptors, messageCount, logProvider,
                                Helpers.GenerateData,
                                Helpers.Verify, true, oCreation.Scope, enableChaos);
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
