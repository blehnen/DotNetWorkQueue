using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.ConsumerMethod
{
    [Collection("Consumer")]
    public class ConsumerMethodPoisonMessage
    {
        [Theory]
        [InlineData(1, 60, 1, true, LinqMethodTypes.Compiled, false),
#if NETFULL
        InlineData(1, 60, 1, false, LinqMethodTypes.Dynamic, false),
         InlineData(10, 60, 5, true, LinqMethodTypes.Dynamic, false),
#endif
         InlineData(3, 60, 5, false, LinqMethodTypes.Compiled, true)]
        public void Run(int messageCount, int timeOut, int workerCount, 
            bool useTransactions, LinqMethodTypes linqMethodTypes, bool enableChaos)
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
                        var id = Guid.NewGuid();
                        //create data
                        var producer = new ProducerMethodShared();
                        if (linqMethodTypes == LinqMethodTypes.Compiled)
                        {
                            producer.RunTestCompiled<SqlServerMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                           Helpers.Verify, false, id, GenerateMethod.CreateNoOpCompiled, 0, oCreation.Scope, false);
                        }
#if NETFULL
                        else
                        {
                            producer.RunTestDynamic<SqlServerMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                           Helpers.Verify, false, id, GenerateMethod.CreateNoOpDynamic, 0, oCreation.Scope, false);
                        }
#endif
                        //process data
                        var consumer = new ConsumerMethodPoisonMessageShared();

                        consumer.RunConsumer<SqlServerMessageQueueInit>(queueConnection,
                            false,
                            workerCount,
                            logProvider, timeOut, messageCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)", enableChaos);

                        ValidateErrorCounts(queueConnection, messageCount);
                        new VerifyQueueRecordCount(queueConnection, oCreation.Options).Verify(messageCount, true, true);
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

        private void ValidateErrorCounts(QueueConnection queueConnection, long messageCount)
        {
            //poison messages are moved to the error queue right away
            //they don't update the tracking table, so specify 0 for the error count.
            //They still update the error table itself
            new VerifyErrorCounts(queueConnection).Verify(messageCount, 0);
        }
    }
}
