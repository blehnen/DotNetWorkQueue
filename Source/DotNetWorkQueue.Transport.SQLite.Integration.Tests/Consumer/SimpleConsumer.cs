using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Consumer
{
    [Collection("Consumer")]
    public class SimpleConsumer
    {
        [Theory]
        [InlineData(1000, 0, 60, 5, true, false),
        InlineData(10, 45, 120, 5, false, false),
        InlineData(10, 0, 60, 5, true, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, bool inMemoryDb, bool enableChaos)
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

                            var producer = new ProducerShared();
                            producer.RunTest<SqLiteMessageQueueInit, FakeMessage>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, oCreation.Scope, false);

                            var consumer = new ConsumerShared<FakeMessage>();
                            consumer.RunConsumer<SqLiteMessageQueueInit>(queueConnection,
                                false,
                                logProvider,
                                runtime, messageCount,
                                workerCount, timeOut,
                                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", enableChaos);

                            new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options).Verify(0, false, false);
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
