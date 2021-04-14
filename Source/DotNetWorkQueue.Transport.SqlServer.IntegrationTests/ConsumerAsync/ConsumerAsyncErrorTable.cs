using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.ConsumerAsync
{
    [Collection("ConsumerAsync")]
    public class ConsumerAsyncErrorTable
    {
        [Theory]
        [InlineData(1, 60, 1, 1, 0, false, false),
        InlineData(25, 120, 20, 1, 5, false, false),
        InlineData(1, 60, 1, 1, 0, true, false),
        InlineData(25, 120, 20, 1, 5, true, false),
        InlineData(2, 120, 20, 1, 5, false, true),
        InlineData(1, 60, 1, 1, 0, true, true)]
        public void Run(int messageCount, int timeOut, int workerCount, 
            int readerCount, int queueSize, bool useTransactions, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (
                var queueCreator =
                    new QueueCreationContainer<SqlServerMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, ConnectionInfo.ConnectionString);
                try
                {

                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection)
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
                        producer.RunTest<SqlServerMessageQueueInit, FakeMessage>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, oCreation.Scope, false);

                        //process data
                        var consumer = new ConsumerAsyncErrorShared<FakeMessage>();
                        consumer.RunConsumer<SqlServerMessageQueueInit>(queueConnection,
                            false,
                            logProvider,
                            messageCount, workerCount, timeOut, queueSize, readerCount,
                            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)", null, enableChaos, new CreationScopeNoOp());
                        ValidateErrorCounts(queueConnection, messageCount);
                        new VerifyQueueRecordCount(queueConnection, oCreation.Options).Verify(messageCount, true, false);

                        consumer.PurgeErrorMessages<SqlServerMessageQueueInit>(queueConnection,
                            false, logProvider, false, new CreationScopeNoOp());
                        ValidateErrorCounts(queueConnection, messageCount);

                        //purge error messages and verify that count is 0
                        consumer.PurgeErrorMessages<SqlServerMessageQueueInit>(queueConnection,
                            false, logProvider, true, new CreationScopeNoOp());
                        ValidateErrorCounts(queueConnection, 0);
                    }
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }
        private void ValidateErrorCounts(QueueConnection queueConnection, int messageCount)
        {
            new VerifyErrorCounts(queueConnection).Verify(messageCount, 2);
        }
    }
}
