using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.ConsumerAsync
{
    [Collection("ConsumerAsync")]
    public class ConsumerAsyncErrorTable
    {
        [Theory]
        [InlineData(1, 60, 1, 1, 0, true, false),
        InlineData(25, 200, 20, 1, 5, false, false),
        InlineData(5, 60, 20, 1, 5, false, true)]
        public void Run(int messageCount, int timeOut, int workerCount, 
            int readerCount, int queueSize, bool inMemoryDb, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (
                    var queueCreator =
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
                            var consumer = new ConsumerAsyncErrorShared<FakeMessage>();
                            consumer.RunConsumer<SqLiteMessageQueueInit>(queueName,connectionInfo.ConnectionString,
                                false,
                                logProvider,
                                messageCount, workerCount, timeOut, queueSize, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", null, enableChaos);
                            ValidateErrorCounts(queueName, connectionInfo.ConnectionString, messageCount);
                            new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options).Verify(messageCount, true, false);
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
