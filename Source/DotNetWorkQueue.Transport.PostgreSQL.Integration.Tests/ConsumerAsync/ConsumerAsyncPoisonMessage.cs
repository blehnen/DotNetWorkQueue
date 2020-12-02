using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.ConsumerAsync
{
    [Collection("consumerasync")]
    public class ConsumerAsyncPoisonMessage
    {
        [Theory]
        [InlineData(10, 60, 5, 1, 0, false, false),
         InlineData(50, 60, 20, 2, 2, false, false),
         InlineData(10, 60, 5, 1, 0, true, false),
         InlineData(50, 60, 20, 2, 2, true, false),
         InlineData(5, 60, 5, 1, 0, false, true)]
        public void Run(int messageCount, int timeOut, int workerCount, int readerCount, int queueSize,
            bool useTransactions, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (var queueCreator =
                new QueueCreationContainer<PostgreSqlMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);
                try
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection)
                        )
                    {
                        oCreation.Options.EnableDelayedProcessing = true;
                        oCreation.Options.EnableHeartBeat = !useTransactions;
                        oCreation.Options.EnableHoldTransactionUntilMessageCommitted = useTransactions;
                        oCreation.Options.EnableStatus = !useTransactions;
                        oCreation.Options.EnableStatusTable = true;

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);

                        //create data
                        var producer = new ProducerShared();
                        producer.RunTest<PostgreSqlMessageQueueInit, FakeMessage>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, oCreation.Scope, false);

                        //process data
                        var consumer = new ConsumerAsyncPoisonMessageShared<FakeMessage>();
                        consumer.RunConsumer<PostgreSqlMessageQueueInit>(queueConnection,
                            false,
                            workerCount, logProvider,
                            timeOut, readerCount, queueSize, messageCount,
                            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)", null, enableChaos);

                        ValidateErrorCounts(queueName, messageCount);
                        new VerifyQueueRecordCount(queueName, oCreation.Options).Verify(messageCount, true, true);
                    }
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }

        private void ValidateErrorCounts(string queueName, long messageCount)
        {
            //poison messages are moved to the error queue right away
            //they don't update the tracking table, so specify 0 for the error count.
            //They still update the error table itself
            new VerifyErrorCounts(queueName).Verify(messageCount, 0);
        }
    }
}
