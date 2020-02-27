using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Consumer
{
    [Collection("Consumer")]
    public class ConsumerErrorTable
    {
        [Theory]
        [InlineData(10, 120, 5, false, false),
         InlineData(10, 120, 5, true, false),
         InlineData(1, 120, 5, true, true)]
        public void Run(int messageCount, int timeOut, int workerCount, bool inMemoryDb, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
                    new QueueCreationContainer<SqLiteMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    try
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
                            )
                        {
                            oCreation.Options.EnableDelayedProcessing = true;
                            oCreation.Options.EnableHeartBeat = true;
                            oCreation.Options.EnableStatus = true;
                            oCreation.Options.EnableStatusTable = true;

                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            //create data
                            var producer = new ProducerShared();
                            producer.RunTest<SqLiteMessageQueueInit, FakeMessage>(queueName,
                                connectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, oCreation.Scope, false);

                            //process data
                            var consumer = new ConsumerErrorShared<FakeMessage>();
                            consumer.RunConsumer<SqLiteMessageQueueInit>(queueName, connectionInfo.ConnectionString,
                                false,
                                logProvider,
                                workerCount, timeOut, messageCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", null, enableChaos);
                            ValidateErrorCounts(queueName, connectionInfo.ConnectionString, messageCount);
                            new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options).Verify(messageCount, true, false);

                            consumer.PurgeErrorMessages<SqLiteMessageQueueInit>(queueName, connectionInfo.ConnectionString,
                                false, logProvider, false);

                            //table should be empty now
                            ValidateErrorCounts(queueName, connectionInfo.ConnectionString, messageCount);

                            //purge error records
                            consumer.PurgeErrorMessages<SqLiteMessageQueueInit>(queueName, connectionInfo.ConnectionString,
                                false, logProvider, true);

                            //table should be empty now
                            ValidateErrorCounts(queueName, connectionInfo.ConnectionString, 0);
                        }
                    }
                    finally
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
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
