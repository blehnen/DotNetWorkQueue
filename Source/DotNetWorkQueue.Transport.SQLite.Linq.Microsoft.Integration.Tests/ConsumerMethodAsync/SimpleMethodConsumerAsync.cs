using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using DotNetWorkQueue.Transport.SQLite.Microsoft.Basic;
using DotNetWorkQueue.Transport.SQLite.Microsoft.Integration.Tests;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Microsoft.Integration.Tests.ConsumerMethodAsync
{
    [Collection("SQLite")]
    public class SimpleMethodConsumerAsync
    {
        private ITaskFactory Factory { get; set; }

        [Theory]
        [InlineData(500, 1, 400, 10, 5, 5, 1, false, LinqMethodTypes.Compiled, false),
         InlineData(5, 5, 200, 10, 1, 2, 1, true, LinqMethodTypes.Compiled, true)]
        public void Run(int messageCount, int runtime, int timeOut,
            int workerCount, int readerCount, int queueSize,
           int messageType, bool inMemoryDb, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            if (Factory == null)
            {
                Factory = CreateFactory(workerCount, queueSize);
            }

            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
                    new QueueCreationContainer<SqLiteMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    try
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
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
                                var id = Guid.NewGuid();
                                var producer = new ProducerMethodAsyncShared();
                                producer.RunTestAsync<SqLiteMessageQueueInit>(queueName,
                                    connectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, runtime, id, linqMethodTypes, oCreation.Scope, enableChaos).Wait(timeOut);

                                var consumer = new ConsumerMethodAsyncShared { Factory = Factory };
                                consumer.RunConsumer<SqLiteMessageQueueInit>(queueName, connectionInfo.ConnectionString,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id, "second(*%10)", enableChaos);
                            }
                            else if (messageType == 2)
                            {
                                var id = Guid.NewGuid();
                                var producer = new ProducerMethodAsyncShared();
                                producer.RunTestAsync<SqLiteMessageQueueInit>(queueName,
                                    connectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, runtime, id, linqMethodTypes, oCreation.Scope, enableChaos).Wait(timeOut);

                                var consumer = new ConsumerMethodAsyncShared { Factory = Factory };
                                consumer.RunConsumer<SqLiteMessageQueueInit>(queueName, connectionInfo.ConnectionString,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id, "second(*%10)", enableChaos);
                            }
                            else if (messageType == 3)
                            {
                                var id = Guid.NewGuid();
                                var producer = new ProducerMethodAsyncShared();
                                producer.RunTestAsync<SqLiteMessageQueueInit>(queueName,
                                    connectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, runtime, id, linqMethodTypes, oCreation.Scope, enableChaos).Wait(timeOut);

                                var consumer = new ConsumerMethodAsyncShared { Factory = Factory };
                                consumer.RunConsumer<SqLiteMessageQueueInit>(queueName, connectionInfo.ConnectionString,
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
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueName,
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
            int messageType, bool inMemoryDb, ITaskFactory factory, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            Factory = factory;
            Run(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, inMemoryDb, linqMethodTypes, enableChaos);
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
