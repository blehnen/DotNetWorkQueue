using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.ConsumerAsync
{
    [Collection("consumerasync")]
    public class ConsumerAsyncErrorTable
    {
        [Theory]
        [InlineData(1, 20, 1, 1, 0),
        InlineData(25, 30, 20, 1, 5)]
        public void Run(int messageCount, int timeOut, int workerCount, int readerCount, int queueSize)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (
                    var queueCreator =
                        new QueueCreationContainer<MemoryMessageQueueInit>(
                            serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    try
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<MessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
                            )
                        {
                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            //create data
                            var producer = new ProducerShared();
                            producer.RunTest<MemoryMessageQueueInit, FakeMessage>(queueName,
                                connectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, oCreation.Scope, false);

                            //process data
                            var consumer = new ConsumerAsyncErrorShared<FakeMessage>();
                            consumer.RunConsumer<MemoryMessageQueueInit>(queueName,connectionInfo.ConnectionString,
                                false,
                                logProvider,
                                messageCount, workerCount, timeOut, queueSize, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", null, false);
                            ValidateErrorCounts(oCreation.Scope, messageCount);
                            new VerifyQueueRecordCount().Verify(oCreation.Scope, messageCount, false);
                        }
                    }
                    finally
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<MessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
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
            new VerifyErrorCounts().Verify(scope, messageCount, 2);
        }
    }
}
