using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync.Implementation
{
    [Collection("ConsumerAsync")]
    public class SimpleMethodConsumerAsync
    {
        private ITaskFactory Factory { get; set; }

        public void Run<TTransportInit, TTransportCreate>(
            string queueName,
            string connectionString,
            int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            int messageType,
            LinqMethodTypes linqMethodTypes,
            bool enableChaos,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            Action<string, string, IBaseTransportOptions, ICreationScope, int, bool, bool> verifyQueueCount)
            where TTransportInit : ITransportInit, new()
            where TTransportCreate : class, IQueueCreation
        {
            if (Factory == null)
            {
                Factory = CreateFactory(workerCount, queueSize);
            }

            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (var queueCreator =
                new QueueCreationContainer<TTransportInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var queueConnection =
                    new DotNetWorkQueue.Configuration.QueueConnection(queueName, connectionString);
                ICreationScope scope = null;
                var oCreation = queueCreator.GetQueueCreation<TTransportCreate>(queueConnection);
                try
                {
                    setOptions(oCreation);
                    var result = oCreation.CreateQueue();
                    Assert.True(result.Success, result.ErrorMessage);
                    scope = oCreation.Scope;

                    if (messageType == 1)
                    {
                        var id = Guid.NewGuid();
                        var producer = new ProducerMethodAsyncShared();
                        producer.RunTestAsync<TTransportInit>(queueConnection, false, messageCount, logProvider,
                            generateData,
                            verify, false, runtime, id, linqMethodTypes, oCreation.Scope, false).Wait(timeOut);

                        var consumer = new ConsumerMethodAsyncShared {Factory = Factory};
                        consumer.RunConsumer<TTransportInit>(queueConnection,
                            false, logProvider,
                            runtime, messageCount,
                            timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id,
                            "second(*%10)", enableChaos, scope);
                    }
                    else if (messageType == 2)
                    {
                        var id = Guid.NewGuid();
                        var producer = new ProducerMethodAsyncShared();
                        producer.RunTestAsync<TTransportInit>(queueConnection, false, messageCount, logProvider,
                            generateData,
                            verify, false, runtime, id, linqMethodTypes, oCreation.Scope, false).Wait(timeOut);

                        var consumer = new ConsumerMethodAsyncShared {Factory = Factory};
                        consumer.RunConsumer<TTransportInit>(queueConnection,
                            false, logProvider,
                            runtime, messageCount,
                            timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id,
                            "second(*%10)", enableChaos, scope);
                    }
                    else if (messageType == 3)
                    {
                        var id = Guid.NewGuid();
                        var producer = new ProducerMethodAsyncShared();
                        producer.RunTestAsync<TTransportInit>(queueConnection, false, messageCount, logProvider,
                            generateData,
                            verify, false, runtime, id, linqMethodTypes, oCreation.Scope, false).Wait(timeOut);

                        var consumer = new ConsumerMethodAsyncShared {Factory = Factory};
                        consumer.RunConsumer<TTransportInit>(queueConnection,
                            false, logProvider,
                            runtime, messageCount,
                            timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id,
                            "second(*%10)", enableChaos, scope);
                    }

                    verifyQueueCount(queueName, connectionString, oCreation.BaseTransportOptions, scope, 0, false, false);
                }
                finally
                {

                    oCreation.RemoveQueue();
                    oCreation.Dispose();
                    scope?.Dispose();
                }
            }
        }

        public void RunWithFactory<TTransportInit, TTransportCreate>(
            string queueName,
            string connectionString,
            int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            int messageType, ITaskFactory factory,
            LinqMethodTypes linqMethodTypes,
            bool enableChaos,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            Action<string, string, IBaseTransportOptions, ICreationScope, int, bool, bool> verifyQueueCount)
            where TTransportInit : ITransportInit, new()
            where TTransportCreate : class, IQueueCreation
        {
            Factory = factory;
            Run<TTransportInit, TTransportCreate>(queueName, connectionString, messageCount, runtime, timeOut, workerCount, readerCount,
                queueSize, messageType, linqMethodTypes, enableChaos, setOptions, generateData,
                verify, verifyQueueCount);
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
