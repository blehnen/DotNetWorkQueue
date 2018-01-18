using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.ConsumerAsync
{
    [Collection("postgresql")]
    public class SimpleConsumerAsync
    {
        private ITaskFactory Factory { get; set; }

        [Theory]
        [InlineData(500, 1, 400, 10, 5, 5, false, 1),
         InlineData(500, 1, 400, 10, 5, 5, true, 1),
         InlineData(500, 0, 180, 10, 5, 0, false, 1),
         InlineData(500, 0, 180, 10, 5, 0, true, 1)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            bool useTransactions, int messageType)
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
                try
                {

                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueName,
                                ConnectionInfo.ConnectionString)
                        )
                    {
                        oCreation.Options.EnableDelayedProcessing = true;
                        oCreation.Options.EnableHeartBeat = !useTransactions;
                        oCreation.Options.EnableHoldTransactionUntilMessageCommitted = useTransactions;
                        oCreation.Options.EnableStatus = !useTransactions;
                        oCreation.Options.EnableStatusTable = true;

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);

                        if (messageType == 1)
                        {
                            var producer = new ProducerAsyncShared();
                            producer.RunTestAsync<PostgreSqlMessageQueueInit, FakeMessage>(queueName,
                                ConnectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, oCreation.Scope).Wait(timeOut / 2 * 1000);

                            var consumer = new ConsumerAsyncShared<FakeMessage> {Factory = Factory};
                            consumer.RunConsumer<PostgreSqlMessageQueueInit>(queueName, ConnectionInfo.ConnectionString,
                                false, logProvider,
                                runtime, messageCount,
                                timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)");
                        }
                        else if (messageType == 2)
                        {
                            var producer = new ProducerAsyncShared();
                            producer.RunTestAsync<PostgreSqlMessageQueueInit, FakeMessageA>(queueName,
                                ConnectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, oCreation.Scope).Wait(timeOut / 2 * 1000);

                            var consumer = new ConsumerAsyncShared<FakeMessageA> {Factory = Factory};
                            consumer.RunConsumer<PostgreSqlMessageQueueInit>(queueName, ConnectionInfo.ConnectionString,
                                false, logProvider,
                                runtime, messageCount,
                                timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)");
                        }
                        else if (messageType == 3)
                        {
                            var producer = new ProducerAsyncShared();
                            producer.RunTestAsync<PostgreSqlMessageQueueInit, FakeMessageB>(queueName,
                                ConnectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, oCreation.Scope).Wait(timeOut / 2 * 1000);

                            var consumer = new ConsumerAsyncShared<FakeMessageB> {Factory = Factory};
                            consumer.RunConsumer<PostgreSqlMessageQueueInit>(queueName, ConnectionInfo.ConnectionString,
                                false, logProvider,
                                runtime, messageCount,
                                timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)");
                        }

                        new VerifyQueueRecordCount(queueName, oCreation.Options).Verify(0, false, false);
                    }
                }
                finally
                {
                    schedulerContainer?.Dispose();
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueName,
                                ConnectionInfo.ConnectionString)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }

#pragma warning disable xUnit1013 // Public method should be marked as test
        public void RunWithFactory(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
#pragma warning restore xUnit1013 // Public method should be marked as test
            bool useTransactions, int messageType, ITaskFactory factory)
        {
            Factory = factory;
            Run(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, useTransactions, messageType);
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
