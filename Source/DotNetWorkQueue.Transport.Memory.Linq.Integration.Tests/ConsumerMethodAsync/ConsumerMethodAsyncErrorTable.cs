using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ConsumerMethodAsync
{
    [Collection("consumer")]
    public class ConsumerMethodAsyncErrorTable
    {
        [Theory]
#if NETFULL
        [InlineData(1, 20, 1, 1, 0, LinqMethodTypes.Dynamic),
        InlineData(25, 60, 20, 1, 5, LinqMethodTypes.Compiled)]
#else
        [InlineData(1, 20, 1, 1, 0, LinqMethodTypes.Compiled)]
#endif
        public void Run(int messageCount, int timeOut, int workerCount, 
            int readerCount, int queueSize, LinqMethodTypes linqMethodTypes)
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
                            var producer = new ProducerMethodShared();
                            var id = Guid.NewGuid();
                            if (linqMethodTypes == LinqMethodTypes.Compiled)
                            {
                                producer.RunTestCompiled<MemoryMessageQueueInit>(queueName,
                                    connectionInfo.ConnectionString, false, messageCount, logProvider,
                                    Helpers.GenerateData,
                                    Helpers.Verify, false, id, GenerateMethod.CreateErrorCompiled, 0, oCreation.Scope, false);
                            }
#if NETFULL
                            else
                            {
                                producer.RunTestDynamic<MemoryMessageQueueInit>(queueName,
                                   connectionInfo.ConnectionString, false, messageCount, logProvider,
                                   Helpers.GenerateData,
                                   Helpers.Verify, false, id, GenerateMethod.CreateErrorDynamic, 0, oCreation.Scope, false);
                            }
#endif
                            //process data
                            var consumer = new ConsumerMethodAsyncErrorShared();
                            consumer.RunConsumer<MemoryMessageQueueInit>(queueName,connectionInfo.ConnectionString,
                                false,
                                logProvider,
                                messageCount, workerCount, timeOut, queueSize, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id, "second(*%10)", false);
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
            new VerifyErrorCounts().Verify(scope, messageCount, 1);
        }
    }
}
