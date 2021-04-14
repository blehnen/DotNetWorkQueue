using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.ConsumerAsync
{
    [Collection("consumerasync")]
    public class SimpleConsumerAsync
    {
        private ITaskFactory Factory { get; set; }

        [Theory]
        [InlineData(500, 1, 400, 10, 5, 5, false, 1, false),
         InlineData(500, 1, 400, 10, 5, 5, true, 1, false),
         InlineData(500, 0, 180, 10, 5, 0, false, 1, false),
         InlineData(500, 0, 180, 10, 5, 0, true, 1, false),
         InlineData(50, 0, 180, 10, 5, 0, false, 1, true),
         InlineData(50, 0, 180, 10, 5, 0, true, 1, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            bool useTransactions, int messageType, bool enableChaos)
        {
            SchedulerContainer schedulerContainer = null;
            if (Factory == null)
            {
                Factory = CreateFactory(workerCount, queueSize, out schedulerContainer);
            }

            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (var queueCreator =
                new QueueCreationContainer<PostgreSqlMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);
                ICreationScope scope = null;
                var oCreation = queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
                try
                {
                    oCreation.Options.EnableDelayedProcessing = true;
                    oCreation.Options.EnableHeartBeat = !useTransactions;
                    oCreation.Options.EnableHoldTransactionUntilMessageCommitted = useTransactions;
                    oCreation.Options.EnableStatus = !useTransactions;
                    oCreation.Options.EnableStatusTable = true;

                    var result = oCreation.CreateQueue();
                    Assert.True(result.Success, result.ErrorMessage);
                    scope = oCreation.Scope;

                    if (messageType == 1)
                    {
                        var producer = new ProducerAsyncShared();
                        producer.RunTestAsync<PostgreSqlMessageQueueInit, FakeMessage>(queueConnection, false,
                            messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, oCreation.Scope, false).Wait(timeOut / 2 * 1000);

                        var consumer = new ConsumerAsyncShared<FakeMessage> {Factory = Factory};
                        consumer.RunConsumer<PostgreSqlMessageQueueInit>(queueConnection,
                            false, logProvider,
                            runtime, messageCount,
                            timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)",
                            enableChaos, scope, null);
                    }
                    else if (messageType == 2)
                    {
                        var producer = new ProducerAsyncShared();
                        producer.RunTestAsync<PostgreSqlMessageQueueInit, FakeMessageA>(queueConnection, false,
                            messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, oCreation.Scope, false).Wait(timeOut / 2 * 1000);

                        var consumer = new ConsumerAsyncShared<FakeMessageA> {Factory = Factory};
                        consumer.RunConsumer<PostgreSqlMessageQueueInit>(queueConnection,
                            false, logProvider,
                            runtime, messageCount,
                            timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)",
                            enableChaos, scope, null);
                    }
                    else if (messageType == 3)
                    {
                        var producer = new ProducerAsyncShared();
                        producer.RunTestAsync<PostgreSqlMessageQueueInit, FakeMessageB>(queueConnection, false,
                            messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, oCreation.Scope, false).Wait(timeOut / 2 * 1000);

                        var consumer = new ConsumerAsyncShared<FakeMessageB> {Factory = Factory};
                        consumer.RunConsumer<PostgreSqlMessageQueueInit>(queueConnection,
                            false, logProvider,
                            runtime, messageCount,
                            timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)",
                            enableChaos, scope, null);
                    }

                    new VerifyQueueRecordCount(queueName, oCreation.Options).Verify(0, false, false);
                }
                finally
                {
                    schedulerContainer?.Dispose();
                    oCreation.RemoveQueue();
                    oCreation.Dispose();
                    scope?.Dispose();
                }
            }
        }

#pragma warning disable xUnit1013 // Public method should be marked as test
        public void RunWithFactory(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
#pragma warning restore xUnit1013 // Public method should be marked as test
            bool useTransactions, int messageType, ITaskFactory factory, bool enableChaos)
        {
            Factory = factory;
            Run(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, useTransactions, messageType, enableChaos);
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
