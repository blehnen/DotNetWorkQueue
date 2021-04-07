using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.ConsumerMethodAsync
{
    [Collection("ConsumerAsync")]
    public class SimpleMethodConsumerAsync
    {
        private ITaskFactory Factory { get; set; }

        [Theory]
        [InlineData(10, 45, 260, 7, 1, 1, 1, LinqMethodTypes.Dynamic, false),
         InlineData(50, 5, 200, 10, 1, 2, 1, LinqMethodTypes.Compiled, false)]
        public void Run(int messageCount, int runtime, int timeOut,
            int workerCount, int readerCount, int queueSize,
           int messageType, LinqMethodTypes linqMethodTypes, bool enableChaos)
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
                    new QueueCreationContainer<LiteDbMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, connectionInfo.ConnectionString);
                    try
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection)
                            )
                        {
                            oCreation.Options.EnableDelayedProcessing = true;
                            oCreation.Options.EnableStatusTable = true;

                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            if (messageType == 1)
                            {
                                var id = Guid.NewGuid();
                                var producer = new ProducerMethodAsyncShared();
                                producer.RunTestAsync<LiteDbMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, runtime, id, linqMethodTypes, oCreation.Scope, false).Wait(timeOut);

                                var consumer = new ConsumerMethodAsyncShared { Factory = Factory };
                                consumer.RunConsumer<LiteDbMessageQueueInit>(queueConnection,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id, "second(*%10)", enableChaos);
                            }
                            else if (messageType == 2)
                            {
                                var id = Guid.NewGuid();
                                var producer = new ProducerMethodAsyncShared();
                                producer.RunTestAsync<LiteDbMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, runtime, id, linqMethodTypes, oCreation.Scope, false).Wait(timeOut);

                                var consumer = new ConsumerMethodAsyncShared { Factory = Factory };
                                consumer.RunConsumer<LiteDbMessageQueueInit>(queueConnection,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id, "second(*%10)", enableChaos);
                            }
                            else if (messageType == 3)
                            {
                                var id = Guid.NewGuid();
                                var producer = new ProducerMethodAsyncShared();
                                producer.RunTestAsync<LiteDbMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, runtime, id, linqMethodTypes, oCreation.Scope, false).Wait(timeOut);

                                var consumer = new ConsumerMethodAsyncShared { Factory = Factory };
                                consumer.RunConsumer<LiteDbMessageQueueInit>(queueConnection,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id, "second(*%10)", enableChaos);
                            }

                            new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options).Verify(0, false, false);
                        }
                    }
                    finally
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection)
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
            int messageType, ITaskFactory factory, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            Factory = factory;
            Run(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, linqMethodTypes, enableChaos);
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
