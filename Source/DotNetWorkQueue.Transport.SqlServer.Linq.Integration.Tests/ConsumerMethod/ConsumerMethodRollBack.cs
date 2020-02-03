using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.ConsumerMethod
{
    [Collection("Consumer")]
    public class ConsumerMethodRollBack
    {

        [Theory]
        [InlineData(50, 5, 200, 10, false, LinqMethodTypes.Compiled, false),
#if NETFULL
        InlineData(50, 5, 200, 10, true, LinqMethodTypes.Dynamic, false),
        InlineData(10, 15, 180, 7, false, LinqMethodTypes.Dynamic, false),
#endif
        InlineData(10, 15, 180, 7, true, LinqMethodTypes.Compiled, false),
        InlineData(3, 15, 180, 7, true, LinqMethodTypes.Compiled, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, 
            bool useTransactions, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (
                var queueCreator =
                    new QueueCreationContainer<SqlServerMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {

                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueName,
                                ConnectionInfo.ConnectionString)
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
                            producer.RunTestCompiled<SqlServerMessageQueueInit>(queueName,
                            ConnectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, id, GenerateMethod.CreateRollBackCompiled, runtime, oCreation.Scope, enableChaos);
                        }
#if NETFULL
                        else
                        {
                            producer.RunTestDynamic<SqlServerMessageQueueInit>(queueName,
                            ConnectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, id, GenerateMethod.CreateRollBackDynamic, runtime, oCreation.Scope, enableChaos);
                        }
#endif
                        //process data
                        var consumer = new ConsumerMethodRollBackShared();
                        consumer.RunConsumer<SqlServerMessageQueueInit>(queueName, ConnectionInfo.ConnectionString,
                            false,
                            workerCount, logProvider, timeOut, runtime, messageCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)", id, enableChaos);

                        new VerifyQueueRecordCount(queueName, oCreation.Options).Verify(0, false, false);
                        GenerateMethod.ClearRollback(id);
                    }
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueName,
                                ConnectionInfo.ConnectionString)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }
    }
}
