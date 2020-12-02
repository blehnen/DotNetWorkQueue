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
    public class ConsumerMethodAsyncRollBack
    {
        [Theory]
        [InlineData(5, 5, 200, 5, 1, 3, false, LinqMethodTypes.Compiled, true),
#if NETFULL
        InlineData(50, 5, 200, 5, 1, 3, false, LinqMethodTypes.Dynamic, false),
         InlineData(10, 5, 180, 7, 1, 1, true, LinqMethodTypes.Dynamic, true),
#endif
         InlineData(10, 5, 180, 7, 1, 1, true, LinqMethodTypes.Compiled, false)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
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
                           Helpers.Verify, false, id, GenerateMethod.CreateRollBackCompiled, runtime, oCreation.Scope, false);
                        }
#if NETFULL
                        else
                        {
                            producer.RunTestDynamic<PostgreSqlMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                           Helpers.Verify, false, id, GenerateMethod.CreateRollBackDynamic, runtime, oCreation.Scope, false);
                        }
#endif
                        //process data
                        var consumer = new ConsumerMethodAsyncRollBackShared();
                        consumer.RunConsumer<PostgreSqlMessageQueueInit>(queueConnection,
                            false,
                            workerCount, logProvider,
                            timeOut, readerCount, queueSize, runtime, messageCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), id, "second(*%3)", enableChaos);
                        LoggerShared.CheckForErrors(queueName);
                        new VerifyQueueRecordCount(queueName, oCreation.Options).Verify(0, false, false);
                        GenerateMethod.ClearRollback(id);
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
