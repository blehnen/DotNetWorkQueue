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
    public class SimpleMethodConsumer
    {
        [Theory]
        [InlineData(10, 0, 240, 5, false, LinqMethodTypes.Compiled, true),
#if NETFULL
       InlineData(100, 0, 240, 5, false, LinqMethodTypes.Dynamic, false),
       InlineData(10, 15, 180, 7, true, LinqMethodTypes.Dynamic, false),
#endif
         InlineData(10, 15, 180, 7, true, LinqMethodTypes.Compiled, false)]
        public void Run(int messageCount, int runtime, int timeOut, 
            int workerCount, bool useTransactions, LinqMethodTypes linqMethodTypes, bool enableChaos)
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
                        var producer = new ProducerMethodShared();
                        if (linqMethodTypes == LinqMethodTypes.Compiled)
                        {
                            producer.RunTestCompiled<PostgreSqlMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, id, GenerateMethod.CreateCompiled, runtime, oCreation.Scope, false);
                        }
#if NETFULL
                        else
                        {
                            producer.RunTestDynamic<PostgreSqlMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, id, GenerateMethod.CreateDynamic, runtime, oCreation.Scope, false);
                        }
#endif
                        var consumer = new ConsumerMethodShared();
                        consumer.RunConsumer<PostgreSqlMessageQueueInit>(queueConnection,
                            false,
                            logProvider,
                            runtime, messageCount,
                            workerCount, timeOut, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), id, "second(*%3)", enableChaos, scope);

                        new VerifyQueueRecordCount(queueName, oCreation.Options).Verify(0, false, false);
                    
                }
                finally
                {
                    oCreation.RemoveQueue();
                    oCreation.Dispose();
                    scope?.Dispose();
                }
            }
        }
    }
}
