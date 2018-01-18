#if NETFULL
using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.ConsumerMethod
{
    public class ConsumerMethodMultipleDynamic
    {
        [Collection("PostgreSQL")]
        public class SimpleMethodConsumer
        {
           [Theory]
           [InlineData(100, 0, 240, 5, false),
           InlineData(200, 0, 240, 25, false),
           InlineData(200, 0, 240, 25, true),
           InlineData(100, 0, 240, 5, true)]
            public void Run(int messageCount, int runtime, int timeOut, int workerCount, bool useTransactions)
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (
                    var queueCreator =
                        new QueueCreationContainer<PostgreSqlMessageQueueInit>(
                            serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    try
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueName,
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
                            var producer = new ProducerMethodMultipleDynamicShared();
                            producer.RunTestDynamic<PostgreSqlMessageQueueInit>(queueName,
                                ConnectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, id, GenerateMethod.CreateMultipleDynamic, runtime, oCreation.Scope);

                            var consumer = new ConsumerMethodShared();
                            consumer.RunConsumer<PostgreSqlMessageQueueInit>(queueName, ConnectionInfo.ConnectionString,
                                false,
                                logProvider,
                                runtime, messageCount,
                                workerCount, timeOut, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), id, "second(*%3)");

                            new VerifyQueueRecordCount(queueName, oCreation.Options).Verify(0, false, false);
                        }
                    }
                    finally
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueName,
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
}
#endif
