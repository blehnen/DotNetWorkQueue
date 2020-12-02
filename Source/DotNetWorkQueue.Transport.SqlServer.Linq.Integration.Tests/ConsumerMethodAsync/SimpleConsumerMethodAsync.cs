using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.ConsumerMethodAsync
{
    [Collection("ConsumerAsync")]
    public class SimpleConsumerMethodAsync
    {
        private ITaskFactory Factory { get; set; }

        [Theory]
        [InlineData(50, 5, 200, 10, 1, 1, true, 1, LinqMethodTypes.Compiled, false),
#if NETFULL
        InlineData(50, 5, 200, 10, 1, 2, false, 1, LinqMethodTypes.Dynamic, false),
        InlineData(10, 5, 180, 7, 1, 1, true, 1, LinqMethodTypes.Dynamic, false),
#endif
        InlineData(10, 5, 180, 7, 1, 2, false, 1, LinqMethodTypes.Compiled, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            bool useTransactions, int messageType, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            if (Factory == null)
            {
                Factory = CreateFactory(workerCount, queueSize);
            }

            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (var queueCreator =
                new QueueCreationContainer<SqlServerMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, ConnectionInfo.ConnectionString);
                try
                {

                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection)
                        )
                    {
                        oCreation.Options.EnableDelayedProcessing = true;
                        oCreation.Options.EnableHeartBeat = !useTransactions;
                        oCreation.Options.EnableHoldTransactionUntilMessageCommitted = useTransactions;
                        oCreation.Options.EnableStatus = !useTransactions;
                        oCreation.Options.EnableStatusTable = true;

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);
                        var id = Guid.NewGuid();
                        if (messageType == 1)
                        {
                            var producer = new ProducerMethodAsyncShared();
                            producer.RunTestAsync<SqlServerMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, runtime, id, linqMethodTypes, oCreation.Scope, false).Wait(timeOut);

                            var consumer = new ConsumerMethodAsyncShared {Factory = Factory};
                            consumer.RunConsumer<SqlServerMessageQueueInit>(queueConnection,
                                false, logProvider,
                                runtime, messageCount,
                                timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), id, "second(*%3)", enableChaos);
                        }
                        else if (messageType == 2)
                        {
                            var producer = new ProducerMethodAsyncShared();
                            producer.RunTestAsync<SqlServerMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, runtime, id, linqMethodTypes, oCreation.Scope, false).Wait(timeOut);

                            var consumer = new ConsumerMethodAsyncShared {Factory = Factory};
                            consumer.RunConsumer<SqlServerMessageQueueInit>(queueConnection,
                                false, logProvider,
                                runtime, messageCount,
                                timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), id, "second(*%3)", enableChaos);
                        }
                        else if (messageType == 3)
                        {
                            var producer = new ProducerMethodAsyncShared();
                            producer.RunTestAsync<SqlServerMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, runtime, id, linqMethodTypes, oCreation.Scope, false).Wait(timeOut);

                            var consumer = new ConsumerMethodAsyncShared {Factory = Factory};
                            consumer.RunConsumer<SqlServerMessageQueueInit>(queueConnection,
                                false, logProvider,
                                runtime, messageCount,
                                timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), id, "second(*%3)", enableChaos);
                        }

                        new VerifyQueueRecordCount(queueConnection, oCreation.Options).Verify(0, false, false);
                    }
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection)
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
            bool useTransactions, int messageType, ITaskFactory factory, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            Factory = factory;
            Run(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, useTransactions, messageType, linqMethodTypes, enableChaos);
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
