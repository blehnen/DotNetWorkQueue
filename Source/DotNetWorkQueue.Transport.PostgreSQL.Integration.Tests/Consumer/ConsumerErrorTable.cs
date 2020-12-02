using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Consumer
{
    [Collection("consumer")]
    public class ConsumerErrorTable
    {
        [Theory]
        [InlineData(100, 120, 40, false, false),
         InlineData(100, 120, 40, true, false),
         InlineData(10, 100, 5, true, false),
         InlineData(10, 120, 40, true, true),
         InlineData(1, 100, 5, false, true)]
        public void Run(int messageCount, int timeOut, int workerCount, bool useTransactions, bool enableChaos)
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
                        var consumer = new ConsumerErrorShared<FakeMessage>();
                        consumer.RunConsumer<PostgreSqlMessageQueueInit>(queueConnection,
                            false,
                            logProvider,
                            workerCount, timeOut, messageCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)", null, enableChaos);
                        ValidateErrorCounts(queueName, messageCount);
                        new VerifyQueueRecordCount(queueName, oCreation.Options).Verify(messageCount, true, false);

                        //run, but don't purge
                        consumer.PurgeErrorMessages<PostgreSqlMessageQueueInit>(queueConnection,
                            false, logProvider, false);
                        ValidateErrorCounts(queueName, messageCount);

                        //purge error records
                        consumer.PurgeErrorMessages<PostgreSqlMessageQueueInit>(queueConnection,
                            false, logProvider, true);

                        //table should be empty now
                        ValidateErrorCounts(queueName, 0);
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

        private void ValidateErrorCounts(string queueName, int messageCount)
        {
            new VerifyErrorCounts(queueName).Verify(messageCount, 2);
        }
    }
}
