using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.ConsumerAsync
{
    [Collection("ConsumerAsync")]
    public class ConsumerAsyncRollBack
    {
        [Theory]
        [InlineData(50, 1, 400, 5, 1, 5,  false),
         InlineData(5, 45, 260, 5, 1, 2, false),
         InlineData(10, 0, 400, 5, 1, 5,  true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, 
            int readerCount, int queueSize, bool enableChaos)
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
                            oCreation.Options.EnableStatusTable = true;
                            oCreation.Options.EnableDelayedProcessing = true;

                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            //create data
                            var producer = new ProducerShared();
                            producer.RunTest<LiteDbMessageQueueInit, FakeMessage>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, oCreation.Scope, false);

                            //process data
                            var consumer = new ConsumerAsyncRollBackShared<FakeMessage>();
                            consumer.RunConsumer<LiteDbMessageQueueInit>(queueConnection,
                                false,
                                workerCount, logProvider,
                                timeOut, readerCount, queueSize, runtime, messageCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", null, enableChaos);
                            LoggerShared.CheckForErrors(queueName);
                            new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options).Verify(0, false, false);
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
