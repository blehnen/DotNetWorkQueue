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
        [InlineData(50, 1, 400, 5, 1, 5,  false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(5, 45, 260, 5, 1, 2, false, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, 0, 400, 5, 1, 5,  true, IntegrationConnectionInfo.ConnectionTypes.Shared)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, 
            int readerCount, int queueSize, bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
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


                        oCreation.Options.EnableStatusTable = true;
                        oCreation.Options.EnableDelayedProcessing = true;

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);
                        scope = oCreation.Scope;

                        //create data
                        var producer = new ProducerShared();
                        producer.RunTest<LiteDbMessageQueueInit, FakeMessage>(queueConnection, false, messageCount,
                            logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, oCreation.Scope, false);

                        //process data
                        var consumer = new ConsumerAsyncRollBackShared<FakeMessage>();
                        consumer.RunConsumer<LiteDbMessageQueueInit>(queueConnection,
                            false,
                            workerCount, logProvider,
                            timeOut, readerCount, queueSize, runtime, messageCount, TimeSpan.FromSeconds(30),
                            TimeSpan.FromSeconds(35), "second(*%10)", null, enableChaos, scope);
                        LoggerShared.CheckForErrors(queueName);
                        new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options, scope)
                            .Verify(0, false, false);

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
