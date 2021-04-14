using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Consumer
{
    [Collection("Consumer")]
    public class ConsumerErrorTable
    {
        [Theory]
        [InlineData(1, 60, 1, false, false),
         InlineData(10, 60, 5, true, false),
         InlineData(3, 60, 5, true, true)]
        public void Run(int messageCount, int timeOut, int workerCount, bool useTransactions, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (var queueCreator =
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
                        var consumer = new ConsumerErrorShared<FakeMessage>();
                        consumer.RunConsumer<SqlServerMessageQueueInit>(queueConnection,
                            false,
                            logProvider,
                            workerCount, timeOut, messageCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)", null, enableChaos, new CreationScopeNoOp());
                        ValidateErrorCounts(queueConnection, messageCount);
                        new VerifyQueueRecordCount(queueConnection, oCreation.Options).Verify(messageCount, true, false);

                        consumer.PurgeErrorMessages<SqlServerMessageQueueInit>(queueConnection,
                            false, logProvider, false, new CreationScopeNoOp());

                        //table should be empty now
                        ValidateErrorCounts(queueConnection, messageCount);

                        //purge error records
                        consumer.PurgeErrorMessages<SqlServerMessageQueueInit>(queueConnection,
                            false, logProvider, true, new CreationScopeNoOp());

                        //table should be empty now
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
