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
        [InlineData(10, 120, 1, false),
         InlineData(1, 120, 1,  true)]
        public void Run(int messageCount, int timeOut, int workerCount, bool enableChaos)
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
                            var consumer = new ConsumerErrorShared<FakeMessage>();
                            consumer.RunConsumer<LiteDbMessageQueueInit>(queueConnection,
                                false,
                                logProvider,
                                workerCount, timeOut, messageCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", null, enableChaos);
                            ValidateErrorCounts(queueName, connectionInfo.ConnectionString, messageCount);
                            new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options).Verify(messageCount, true, false);

                            consumer.PurgeErrorMessages<LiteDbMessageQueueInit>(queueConnection,
                                false, logProvider, false);

                            //table should be empty now
                            ValidateErrorCounts(queueName, connectionInfo.ConnectionString, messageCount);

                            //purge error records
                            consumer.PurgeErrorMessages<LiteDbMessageQueueInit>(queueConnection,
                                false, logProvider, true);

                            //table should be empty now
                            ValidateErrorCounts(queueName, connectionInfo.ConnectionString, 0);
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

        private void ValidateErrorCounts(string queueName, string connectionString, int messageCount)
        {
            new VerifyErrorCounts(queueName, connectionString).Verify(messageCount, 2);
        }
    }
}
