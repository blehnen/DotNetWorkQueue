using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.ConsumerMethodAsync
{
    [Collection("consumerasync")]
    public class ConsumerMethodAsyncErrorTable
    {
        [Theory]
#if NETFULL
        [InlineData(1, 60, 1, 1, 0, false, LinqMethodTypes.Dynamic, true),
        InlineData(1, 60, 1, 1, 0, false, LinqMethodTypes.Compiled, false)]
#else
        [InlineData(1, 60, 1, 1, 0, false, LinqMethodTypes.Compiled, true)]
#endif
        public void Run(int messageCount, int timeOut, int workerCount, 
            int readerCount, int queueSize, bool useTransactions, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (
                var queueCreator =
                    new QueueCreationContainer<PostgreSqlMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);
                ICreationScope scope = null;
                var oCreation = queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
                try
                {

                  
                        oCreation.Options.EnableDelayedProcessing = true;
                        oCreation.Options.EnableHeartBeat = !useTransactions;
                        oCreation.Options.EnableHoldTransactionUntilMessageCommitted = useTransactions;
                        oCreation.Options.EnableStatus = !useTransactions;
                        oCreation.Options.EnableStatusTable = true;

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);
                        scope = oCreation.Scope;
                        var id = Guid.NewGuid();
                        //create data
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
                          Helpers.Verify, false, id, GenerateMethod.CreateErrorDynamic, 0, oCreation.Scope, enableChaos);
                        }
#endif
                        //process data
                        var consumer = new ConsumerMethodAsyncErrorShared();
                        consumer.RunConsumer<PostgreSqlMessageQueueInit>(queueConnection,
                            false,
                            logProvider,
                            messageCount, workerCount, timeOut, queueSize, readerCount,
                            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), id, "second(*%3)", enableChaos, scope);
                        ValidateErrorCounts(queueName, messageCount);
                        new VerifyQueueRecordCount(queueName, oCreation.Options).Verify(messageCount, true, false);

                        consumer.PurgeErrorMessages<PostgreSqlMessageQueueInit>(queueConnection,
                            false, logProvider, false, scope);
                        ValidateErrorCounts(queueName, messageCount);

                        //purge error messages and verify that count is 0
                        consumer.PurgeErrorMessages<PostgreSqlMessageQueueInit>(queueConnection,
                            false,  logProvider, true, scope);
                        ValidateErrorCounts(queueName, 0);
                    
                }
                finally
                {
                    oCreation.RemoveQueue();
                    oCreation.Dispose();
                    scope?.Dispose();
                }
            }
        }
        private void ValidateErrorCounts(string queueName, int messageCount)
        {
            new VerifyErrorCounts(queueName).Verify(messageCount, 2);
        }
    }
}
