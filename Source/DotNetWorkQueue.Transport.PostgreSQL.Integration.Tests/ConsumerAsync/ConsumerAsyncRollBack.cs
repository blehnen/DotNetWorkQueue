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
    public class ConsumerAsyncRollBack
    {
        [Theory]
        [InlineData(100, 1, 400, 5, 5, 5, false, false),
         InlineData(50, 5, 200, 5, 1, 3, false, false),
         InlineData(100, 1, 400, 5, 5, 5, true, false),
         InlineData(50, 5, 200, 5, 1, 3, true, false),
         InlineData(5, 5, 200, 5, 1, 3, true, true),
         InlineData(5, 5, 200, 5, 1, 3, false, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
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
                        var consumer = new ConsumerAsyncRollBackShared<FakeMessage>();
                        consumer.RunConsumer<PostgreSqlMessageQueueInit>(queueConnection,
                            false,
                            workerCount, logProvider,
                            timeOut, readerCount, queueSize, runtime, messageCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)", null, enableChaos);
                        LoggerShared.CheckForErrors(queueName);
                        new VerifyQueueRecordCount(queueName, oCreation.Options).Verify(0, false, false);
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
    }
}
