using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.ConsumerAsync
{
    [Collection("SQLite")]
    public class ConsumerAsyncPoisonMessage
    {
        [Theory]
        [InlineData(1, 20, 1, 1, 0, true, false),
         InlineData(50, 40, 10, 2, 2, false, false),
         InlineData(5, 40, 10, 2, 2, false, true)]
        public void Run(int messageCount, int timeOut, int workerCount, int readerCount, int queueSize, bool inMemoryDb, bool enableChaos)
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
                                Helpers.Verify, false, oCreation.Scope, enableChaos);

                            //process data
                            var consumer = new ConsumerAsyncPoisonMessageShared<FakeMessage>();
                            consumer.RunConsumer<SqLiteMessageQueueInit>(queueName, connectionInfo.ConnectionString,
                                false,
                                workerCount, logProvider,
                                timeOut, readerCount, queueSize, messageCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", null, enableChaos);

                            ValidateErrorCounts(queueName, connectionInfo.ConnectionString, messageCount);
                            new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options).Verify(messageCount, true, true);
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

        private void ValidateErrorCounts(string queueName, string connectionString, long messageCount)
        {
            //poison messages are moved to the error queue right away
            //they don't update the tracking table, so specify 0 for the error count.
            //They still update the error table itself
            new VerifyErrorCounts(queueName, connectionString).Verify(messageCount, 0);
        }
    }
}
