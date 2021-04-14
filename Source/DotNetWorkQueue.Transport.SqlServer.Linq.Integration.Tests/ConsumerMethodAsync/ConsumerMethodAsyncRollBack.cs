using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.ConsumerMethodAsync
{
    [Collection("ConsumerAsync")]
    public class ConsumerMethodAsyncRollBack
    {
        [Theory]
        [InlineData(50, 5, 200, 5, 1, 3, true, LinqMethodTypes.Compiled, false),
#if NETFULL
         InlineData(100, 1, 400, 5, 5, 5, false, LinqMethodTypes.Dynamic, false),
         InlineData(50, 5, 200, 5, 1, 3, true, LinqMethodTypes.Dynamic, false),
#endif
         InlineData(10, 5, 280, 7, 1, 1, false, LinqMethodTypes.Compiled, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
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

                        //create data
                        var id = Guid.NewGuid();
                        var producer = new ProducerMethodShared();
                        if (linqMethodTypes == LinqMethodTypes.Compiled)
                        {
                            producer.RunTestCompiled<SqlServerMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                           Helpers.Verify, false, id, GenerateMethod.CreateRollBackCompiled, runtime, oCreation.Scope, false);
                        }
#if NETFULL
                        else
                        {
                            producer.RunTestDynamic<SqlServerMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                           Helpers.Verify, false, id, GenerateMethod.CreateRollBackDynamic, runtime, oCreation.Scope, false);
                        }
#endif
                        //process data
                        var consumer = new ConsumerMethodAsyncRollBackShared();
                        consumer.RunConsumer<SqlServerMessageQueueInit>(queueConnection,
                            false,
                            workerCount, logProvider,
                            timeOut, readerCount, queueSize, runtime, messageCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), id, "second(*%3)", enableChaos, new CreationScopeNoOp());
                        LoggerShared.CheckForErrors(queueName);
                        new VerifyQueueRecordCount(queueConnection, oCreation.Options).Verify(0, false, false);
                        GenerateMethod.ClearRollback(id);
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
    }
}
