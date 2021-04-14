using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.ConsumerAsync
{
    [Collection("ConsumerAsync")]
    public class ConsumerAsyncPoisonMessage
    {
        [Theory]
        [InlineData(1, 60, 1, 1, 0,  false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(50, 60, 10, 2, 2, false, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(5, 60, 10, 2, 2,  true, IntegrationConnectionInfo.ConnectionTypes.Shared)]
        public void Run(int messageCount, int timeOut, int workerCount, int readerCount, int queueSize, bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
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

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);
                        scope = oCreation.Scope;
                        //create data
                        var producer = new ProducerShared();
                        producer.RunTest<LiteDbMessageQueueInit, FakeMessage>(queueConnection, false, messageCount,
                            logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, oCreation.Scope, false);

                        //process data
                        var consumer = new ConsumerAsyncPoisonMessageShared<FakeMessage>();
                        consumer.RunConsumer<LiteDbMessageQueueInit>(queueConnection,
                            false,
                            workerCount, logProvider,
                            timeOut, readerCount, queueSize, messageCount, TimeSpan.FromSeconds(30),
                            TimeSpan.FromSeconds(35), "second(*%10)", null, enableChaos, scope);

                        ValidateErrorCounts(queueName, connectionInfo.ConnectionString, messageCount, scope);
                        new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options, scope)
                            .Verify(messageCount, true, true);

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

        private void ValidateErrorCounts(string queueName, string connectionString, long messageCount, ICreationScope scope)
        {
            //poison messages are moved to the error queue right away
            //they don't update the tracking table, so specify 0 for the error count.
            //They still update the error table itself
            new VerifyErrorCounts(queueName, connectionString, scope).Verify(messageCount, 0);
        }
    }
}
