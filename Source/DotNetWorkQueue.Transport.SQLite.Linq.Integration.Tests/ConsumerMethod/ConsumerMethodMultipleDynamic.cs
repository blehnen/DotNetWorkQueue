using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.ConsumerMethod
{
    [Collection("Consumer")]
    public class ConsumerMethodMultipleDynamic
    {
        [Theory]
        [InlineData(1000, 0, 240, 5, true, false),
         InlineData(100, 0, 240, 10, true, true),
         InlineData(100, 0, 240, 5, false, true),
         InlineData(1000, 0, 240, 10, false, false)]
        public void Run(int messageCount, int runtime,
            int timeOut, int workerCount, bool inMemoryDb, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (
                    var queueCreator =
                        new QueueCreationContainer<SqLiteMessageQueueInit>(
                            serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, connectionInfo.ConnectionString);
                    try
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection)
                            )
                        {
                            oCreation.Options.EnableDelayedProcessing = true;
                            oCreation.Options.EnableHeartBeat = true;
                            oCreation.Options.EnableStatus = true;
                            oCreation.Options.EnableStatusTable = true;

                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            var producer = new ProducerMethodMultipleDynamicShared();
                            var id = Guid.NewGuid();
                            producer.RunTestDynamic<SqLiteMessageQueueInit>(queueConnection, false, messageCount, logProvider,
                                Helpers.GenerateData,
                                Helpers.Verify, false, id, GenerateMethod.CreateMultipleDynamic, runtime, oCreation.Scope, false);

                            var consumer = new ConsumerMethodShared();
                            consumer.RunConsumer<SqLiteMessageQueueInit>(queueConnection,
                                false,
                                logProvider,
                                runtime, messageCount,
                                workerCount, timeOut,
                                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id, "second(*%10)", enableChaos);

                            new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options)
                                .Verify(0, false, false);
                        }
                    }
                    finally
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection)
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
