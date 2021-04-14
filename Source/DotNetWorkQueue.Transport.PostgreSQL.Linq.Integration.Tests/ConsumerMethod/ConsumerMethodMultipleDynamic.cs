#if NETFULL
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
    public class ConsumerMethodMultipleDynamic
    {
        [Collection("PostgreSQL")]
        public class SimpleMethodConsumer
        {
           [Theory]
           [InlineData(100, 0, 240, 5, false, false),
           InlineData(200, 0, 240, 25, false, false),
           InlineData(200, 0, 240, 25, true, false),
           InlineData(100, 0, 240, 5, true, false),
            InlineData(10, 0, 240, 5, true, true)]
            public void Run(int messageCount, int runtime, int timeOut, 
                int workerCount, bool useTransactions, bool enableChaos)
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
                            var producer = new ProducerMethodMultipleDynamicShared();
                            producer.RunTestDynamic<PostgreSqlMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, id, GenerateMethod.CreateMultipleDynamic, runtime, oCreation.Scope, false);

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
}
#endif
