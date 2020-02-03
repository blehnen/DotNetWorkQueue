using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ConsumerMethodAsync
{
    [Collection("consumer")]
    public class SimpleMethodConsumerAsync
    {
        private ITaskFactory Factory { get; set; }

        [Theory]
#if NETFULL
        [InlineData(10, 15, 60, 7, 1, 1, 1, LinqMethodTypes.Dynamic),
         InlineData(10, 5, 60, 10, 1, 2, 1, LinqMethodTypes.Compiled)]
#else
        [InlineData(10, 5, 60, 10, 1, 2, 1, LinqMethodTypes.Compiled)]
#endif
        public void Run(int messageCount, int runtime, int timeOut,
            int workerCount, int readerCount, int queueSize,
           int messageType, LinqMethodTypes linqMethodTypes)
        {
            if (Factory == null)
            {
                Factory = CreateFactory(workerCount, queueSize);
            }

            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
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

                            if (messageType == 1)
                            {
                                var id = Guid.NewGuid();
                                var producer = new ProducerMethodAsyncShared();
                                producer.RunTestAsync<MemoryMessageQueueInit>(queueName,
                                    connectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, runtime, id, linqMethodTypes, oCreation.Scope, false).Wait(timeOut);

                                var consumer = new ConsumerMethodAsyncShared { Factory = Factory };
                                consumer.RunConsumer<MemoryMessageQueueInit>(queueName, connectionInfo.ConnectionString,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id, "second(*%10)", false);
                            }
                            else if (messageType == 2)
                            {
                                var id = Guid.NewGuid();
                                var producer = new ProducerMethodAsyncShared();
                                producer.RunTestAsync<MemoryMessageQueueInit>(queueName,
                                    connectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, runtime, id, linqMethodTypes, oCreation.Scope, false).Wait(timeOut);

                                var consumer = new ConsumerMethodAsyncShared { Factory = Factory };
                                consumer.RunConsumer<MemoryMessageQueueInit>(queueName, connectionInfo.ConnectionString,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id, "second(*%10)", false);
                            }
                            else if (messageType == 3)
                            {
                                var id = Guid.NewGuid();
                                var producer = new ProducerMethodAsyncShared();
                                producer.RunTestAsync<MemoryMessageQueueInit>(queueName,
                                    connectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, runtime, id, linqMethodTypes, oCreation.Scope, false).Wait(timeOut);

                                var consumer = new ConsumerMethodAsyncShared { Factory = Factory };
                                consumer.RunConsumer<MemoryMessageQueueInit>(queueName, connectionInfo.ConnectionString,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id, "second(*%10)", false);
                            }

                            new VerifyQueueRecordCount().Verify(oCreation.Scope, 0, true);
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

#pragma warning disable xUnit1013 // Public method should be marked as test
        public void RunWithFactory(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
#pragma warning restore xUnit1013 // Public method should be marked as test
            int messageType, ITaskFactory factory, LinqMethodTypes linqMethodTypes)
        {
            Factory = factory;
            Run(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, linqMethodTypes);
        }


        public static ITaskFactory CreateFactory(int maxThreads, int maxQueueSize)
        {
            var schedulerCreator = new SchedulerContainer();
            var taskScheduler = schedulerCreator.CreateTaskScheduler();

            taskScheduler.Configuration.MaximumThreads = maxThreads;
            taskScheduler.Configuration.MaxQueueSize = maxQueueSize;

            taskScheduler.Start();
            return schedulerCreator.CreateTaskFactory(taskScheduler);
        }
    }
}
