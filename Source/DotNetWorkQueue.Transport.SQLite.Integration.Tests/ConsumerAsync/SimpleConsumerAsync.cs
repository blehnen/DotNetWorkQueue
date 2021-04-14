using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.ConsumerAsync
{
    [Collection("ConsumerAsync")]
    public class SimpleConsumerAsync
    {
        private ITaskFactory Factory { get; set; }

        [Theory]
        [InlineData(100, 1, 400, 10, 5, 5, 1, true, false),
         InlineData(100, 1, 400, 10, 5, 5, 1, false, false),
         InlineData(10, 1, 400, 10, 5, 5, 1, false, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
           int messageType, bool inMemoryDb, bool enableChaos)
        {
            SchedulerContainer schedulerContainer = null;
            if (Factory == null)
            {
                Factory = CreateFactory(workerCount, queueSize, out schedulerContainer);
            }


            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
                    new QueueCreationContainer<SqLiteMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    var queueConnection = new QueueConnection(queueName, connectionInfo.ConnectionString);
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

                            if (messageType == 1)
                            {
                                var producer = new ProducerAsyncShared();
                                producer.RunTestAsync<SqLiteMessageQueueInit, FakeMessage>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, oCreation.Scope, false).Wait(timeOut / 2 * 1000);

                                var consumer = new ConsumerAsyncShared<FakeMessage> { Factory = Factory };
                                consumer.RunConsumer<SqLiteMessageQueueInit>(queueConnection,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", enableChaos, new CreationScopeNoOp(), null);
                            }
                            else if (messageType == 2)
                            {
                                var producer = new ProducerAsyncShared();
                                producer.RunTestAsync<SqLiteMessageQueueInit, FakeMessageA>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, oCreation.Scope, false).Wait(timeOut / 2 * 1000);

                                var consumer = new ConsumerAsyncShared<FakeMessageA> { Factory = Factory };
                                consumer.RunConsumer<SqLiteMessageQueueInit>(queueConnection,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", enableChaos, new CreationScopeNoOp(), null);
                            }
                            else if (messageType == 3)
                            {
                                var producer = new ProducerAsyncShared();
                                producer.RunTestAsync<SqLiteMessageQueueInit, FakeMessageB>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, oCreation.Scope, false).Wait(timeOut / 2 * 1000);

                                var consumer = new ConsumerAsyncShared<FakeMessageB> { Factory = Factory };
                                consumer.RunConsumer<SqLiteMessageQueueInit>(queueConnection,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount,
                                    TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", enableChaos, new CreationScopeNoOp(), null);
                            }

                            new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options).Verify(0, false, false);
                        }
                    }
                    finally
                    {
                        schedulerContainer?.Dispose();
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

#pragma warning disable xUnit1013 // Public method should be marked as test
        public void RunWithFactory(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
#pragma warning restore xUnit1013 // Public method should be marked as test
            int messageType, bool inMemoryDb, ITaskFactory factory, bool enableChaos)
        {
            Factory = factory;
            Run(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, inMemoryDb, enableChaos);
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
