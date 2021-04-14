using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.ConsumerAsync
{
    [Collection("ConsumerAsync")]
    public class SimpleConsumerAsync
    {
        private ITaskFactory Factory { get; set; }

        [Theory]
        [InlineData(50, 1, 400, 10, 5, 5, 1,  false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(10, 1, 400, 10, 5, 5, 1,true, IntegrationConnectionInfo.ConnectionTypes.Memory)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
           int messageType, bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            SchedulerContainer schedulerContainer = null;
            if (Factory == null)
            {
                Factory = CreateFactory(workerCount, queueSize, out schedulerContainer);
            }


            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
                    new QueueCreationContainer<LiteDbMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    var queueConnection = new QueueConnection(queueName, connectionInfo.ConnectionString);
                    ICreationScope scope = null;
                    var oCreation = queueCreator.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection);
                    try
                    {


                        oCreation.Options.EnableStatusTable = true;

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);
                        scope = oCreation.Scope;

                        if (messageType == 1)
                        {
                            var producer = new ProducerAsyncShared();
                            producer.RunTestAsync<LiteDbMessageQueueInit, FakeMessage>(queueConnection, false,
                                messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, oCreation.Scope, false).Wait(timeOut / 2 * 1000);

                            var consumer = new ConsumerAsyncShared<FakeMessage> {Factory = Factory};
                            consumer.RunConsumer<LiteDbMessageQueueInit>(queueConnection,
                                false, logProvider,
                                runtime, messageCount,
                                timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35),
                                "second(*%10)", enableChaos, scope, null);
                        }
                        else if (messageType == 2)
                        {
                            var producer = new ProducerAsyncShared();
                            producer.RunTestAsync<LiteDbMessageQueueInit, FakeMessageA>(queueConnection, false,
                                messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, oCreation.Scope, false).Wait(timeOut / 2 * 1000);

                            var consumer = new ConsumerAsyncShared<FakeMessageA> {Factory = Factory};
                            consumer.RunConsumer<LiteDbMessageQueueInit>(queueConnection,
                                false, logProvider,
                                runtime, messageCount,
                                timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35),
                                "second(*%10)", enableChaos, scope, null);
                        }
                        else if (messageType == 3)
                        {
                            var producer = new ProducerAsyncShared();
                            producer.RunTestAsync<LiteDbMessageQueueInit, FakeMessageB>(queueConnection, false,
                                messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, oCreation.Scope, false).Wait(timeOut / 2 * 1000);

                            var consumer = new ConsumerAsyncShared<FakeMessageB> {Factory = Factory};
                            consumer.RunConsumer<LiteDbMessageQueueInit>(queueConnection,
                                false, logProvider,
                                runtime, messageCount,
                                timeOut, readerCount,
                                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", enableChaos, scope,
                                null);
                        }

                        new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options, scope)
                            .Verify(0, false, false);

                    }
                    finally
                    {
                        schedulerContainer?.Dispose();
                        oCreation?.RemoveQueue();
                        oCreation?.Dispose();
                        scope?.Dispose();
                    }
                }
            }
        }

#pragma warning disable xUnit1013 // Public method should be marked as test
        public void RunWithFactory(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
#pragma warning restore xUnit1013 // Public method should be marked as test
            int messageType, ITaskFactory factory, bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            Factory = factory;
            Run(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, enableChaos, connectionType);
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
