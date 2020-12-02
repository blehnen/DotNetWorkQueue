using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.ConsumerMethod
{
    [Collection("consumer")]
    public class ConsumerMethodErrorTable
    {
        [Theory]
        [InlineData(10, 60, 20, true, LinqMethodTypes.Compiled, true),
#if NETFULL
        InlineData(100, 60, 20, false, LinqMethodTypes.Dynamic, false),
         InlineData(100, 60, 20, true, LinqMethodTypes.Dynamic, false),
         InlineData(1, 60, 5, true, LinqMethodTypes.Dynamic, true),
#endif
         InlineData(10, 60, 5, true, LinqMethodTypes.Compiled, false)]
        public void Run(int messageCount, int timeOut, int workerCount, 
            bool useTransactions, LinqMethodTypes linqMethodTypes, bool enableChaos)
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
                        var id = Guid.NewGuid();
                        var producer = new ProducerMethodShared();
                        if (linqMethodTypes == LinqMethodTypes.Compiled)
                        {
                            producer.RunTestCompiled<PostgreSqlMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, id, GenerateMethod.CreateErrorCompiled, 0, oCreation.Scope, false);
                        }
#if NETFULL
                        else
                        {
                            producer.RunTestDynamic<PostgreSqlMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, id, GenerateMethod.CreateErrorDynamic, 0, oCreation.Scope, false);
                        }
#endif
                        //process data
                        var consumer = new ConsumerMethodErrorShared();
                        consumer.RunConsumer<PostgreSqlMessageQueueInit>(queueConnection,
                            false,
                            logProvider,
                            workerCount, timeOut, messageCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), id, "second(*%3)", enableChaos);
                        ValidateErrorCounts(queueName, messageCount);
                        new VerifyQueueRecordCount(queueName, oCreation.Options).Verify(messageCount, true, false);

                        consumer.PurgeErrorMessages<PostgreSqlMessageQueueInit>(queueConnection,
                            false, logProvider, false);
                        ValidateErrorCounts(queueName, messageCount);

                        //purge error messages and verify that count is 0
                        consumer.PurgeErrorMessages<PostgreSqlMessageQueueInit>(queueConnection,
                            false,  logProvider, true);
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
