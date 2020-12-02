using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.ConsumerAsync
{
    [Collection("consumerasync")]
    public class SimpleConsumerAsync
    {
        private ITaskFactory Factory { get; set; }

        [Theory]
        [InlineData(10, 5, 60, 7, 1, 1, 1),
         InlineData(100, 0, 30, 10, 5, 0, 1)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
           int messageType)
        {
            SchedulerContainer schedulerContainer = null;
            if (Factory == null)
            {
                Factory = CreateFactory(workerCount, queueSize, out schedulerContainer);
            }


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

                            if (messageType == 1)
                            {
                                var producer = new ProducerAsyncShared();
                                producer.RunTestAsync<MemoryMessageQueueInit, FakeMessage>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, oCreation.Scope, false).Wait(timeOut / 2 * 1000);

                                var consumer = new ConsumerAsyncShared<FakeMessage> { Factory = Factory };
                                consumer.RunConsumer<MemoryMessageQueueInit>(queueConnection,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", false);
                            }
                            else if (messageType == 2)
                            {
                                var producer = new ProducerAsyncShared();
                                producer.RunTestAsync<MemoryMessageQueueInit, FakeMessageA>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, oCreation.Scope, false).Wait(timeOut / 2 * 1000);

                                var consumer = new ConsumerAsyncShared<FakeMessageA> { Factory = Factory };
                                consumer.RunConsumer<MemoryMessageQueueInit>(queueConnection,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", false);
                            }
                            else if (messageType == 3)
                            {
                                var producer = new ProducerAsyncShared();
                                producer.RunTestAsync<MemoryMessageQueueInit, FakeMessageB>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, oCreation.Scope, false).Wait(timeOut / 2 * 1000);

                                var consumer = new ConsumerAsyncShared<FakeMessageB> { Factory = Factory };
                                consumer.RunConsumer<MemoryMessageQueueInit>(queueConnection,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount,
                                    TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", false);
                            }

                            new VerifyQueueRecordCount().Verify(oCreation.Scope, 0, true);
                        }
                    }
                    finally
                    {
                        schedulerContainer?.Dispose();
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

#pragma warning disable xUnit1013 // Public method should be marked as test
        public void RunWithFactory(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
#pragma warning restore xUnit1013 // Public method should be marked as test
            int messageType, ITaskFactory factory)
        {
            Factory = factory;
            Run(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType);
        }


        public static ITaskFactory CreateFactory(int maxThreads, int maxQueueSize, out SchedulerContainer schedulerCreator)
        {
            schedulerCreator = new SchedulerContainer();
            var taskScheduler = schedulerCreator.CreateTaskScheduler();

            taskScheduler.Configuration.MaximumThreads = maxThreads;
            taskScheduler.Configuration.MaxQueueSize = maxQueueSize;

            taskScheduler.Start();
            return schedulerCreator.CreateTaskFactory(taskScheduler);
        }
    }
}
