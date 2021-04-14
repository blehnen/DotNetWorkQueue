using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.Consumer
{
    [Collection("Consumer")]
    public class ConsumerErrorTable
    {
        [Theory]
        [InlineData(10, 120, 1, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(1, 120, 1,  true, IntegrationConnectionInfo.ConnectionTypes.Memory)]
        public void Run(int messageCount, int timeOut, int workerCount, bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
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
                        var consumer = new ConsumerErrorShared<FakeMessage>();
                        consumer.RunConsumer<LiteDbMessageQueueInit>(queueConnection,
                            false,
                            logProvider,
                            workerCount, timeOut, messageCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35),
                            "second(*%10)", null, enableChaos, scope);
                        ValidateErrorCounts(queueName, connectionInfo.ConnectionString, messageCount, scope);
                        new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options, scope)
                            .Verify(messageCount, true, false);

                        consumer.PurgeErrorMessages<LiteDbMessageQueueInit>(queueConnection,
                            false, logProvider, false, scope);

                        //table should be empty now
                        ValidateErrorCounts(queueName, connectionInfo.ConnectionString, messageCount, scope);

                        //purge error records
                        consumer.PurgeErrorMessages<LiteDbMessageQueueInit>(queueConnection,
                            false, logProvider, true, scope);

                        //table should be empty now
                        ValidateErrorCounts(queueName, connectionInfo.ConnectionString, 0, scope);

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

        private void ValidateErrorCounts(string queueName, string connectionString, int messageCount, ICreationScope scope)
        {
            new VerifyErrorCounts(queueName, connectionString, scope).Verify(messageCount, 2);
        }
    }
}
