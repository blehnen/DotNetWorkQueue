using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.Consumer
{
    [Collection("consumer")]
    public class ConsumerErrorTable
    {
        [Theory]
        [InlineData(10, 20, 5)]
        public void Run(int messageCount, int timeOut, int workerCount)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
                    new QueueCreationContainer<MemoryMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    var queueConnection = new QueueConnection(queueName, connectionInfo.ConnectionString);
                    try
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<MessageQueueCreation>(queueConnection)
                            )
                        {
                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            //create data
                            var producer = new ProducerShared();
                            producer.RunTest<MemoryMessageQueueInit, FakeMessage>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, oCreation.Scope, false);

                            //process data
                            var consumer = new ConsumerErrorShared<FakeMessage>();
                            consumer.RunConsumer<MemoryMessageQueueInit>(queueConnection,
                                false,
                                logProvider,
                                workerCount, timeOut, messageCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", null, false, new CreationScopeNoOp());
                            ValidateErrorCounts(oCreation.Scope, messageCount);
                            new VerifyQueueRecordCount().Verify(oCreation.Scope, messageCount, false);

                            //purge error records
                            consumer.PurgeErrorMessages<MemoryMessageQueueInit>(queueConnection,
                               false,  logProvider, true, new CreationScopeNoOp());

                            //memory queue doesn't actually keep errors, so nothing to check for...
                        }
                    }
                    finally
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<MessageQueueCreation>(queueConnection)
                            )
                        {
                            oCreation.RemoveQueue();
                        }
                    }
                }
            }
        }

        private void ValidateErrorCounts(ICreationScope scope, int messageCount)
        {
            new VerifyErrorCounts().Verify(scope, messageCount, 1);
        }
    }
}
