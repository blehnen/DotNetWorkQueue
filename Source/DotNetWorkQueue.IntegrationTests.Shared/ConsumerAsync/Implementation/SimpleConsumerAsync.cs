using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation
{
    public class SimpleConsumerAsync
    {
        private ITaskFactory Factory { get; set; }

        public async Task Run<TTransportInit, TTransportCreate>(
            QueueConnection queueConnection,
            int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            int messageType, bool enableChaos,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            Action<QueueConnection, IBaseTransportOptions, ICreationScope, int, bool, bool> verifyQueueCount)
            where TTransportInit : ITransportInit, new()
            where TTransportCreate : class, IQueueCreation
        {
            SchedulerContainer schedulerContainer = null;
            if (Factory == null)
            {
                Factory = CreateFactory(workerCount, queueSize, out schedulerContainer);
            }

            var logProvider = LoggerShared.Create(queueConnection.Queue, GetType().Name);
            using (var queueCreator =
                new QueueCreationContainer<TTransportInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                ICreationScope scope = null;
                var oCreation = queueCreator.GetQueueCreation<TTransportCreate>(queueConnection);
                var time = runtime * 1000 / 2;
                try
                {
                    setOptions(oCreation);
                    var result = oCreation.CreateQueue();
                    Assert.True(result.Success, result.ErrorMessage);
                    scope = oCreation.Scope;

                    if (messageType == 1)
                    {
                        var producer = new ProducerAsyncShared();
                        await producer.RunTestAsync<TTransportInit, FakeMessage>(queueConnection, false,
                            messageCount, logProvider, generateData,
                            verify, false, scope, false).ConfigureAwait(false);
                        Thread.Sleep(time);
                        var consumer = new ConsumerAsyncShared<FakeMessage> { Factory = Factory };
                        consumer.RunConsumer<TTransportInit>(queueConnection,
                            false, logProvider,
                            runtime, messageCount,
                            timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35),
                            "second(*%10)", enableChaos, scope, null);
                    }
                    else if (messageType == 2)
                    {
                        var producer = new ProducerAsyncShared();
                        await producer.RunTestAsync<TTransportInit, FakeMessageA>(queueConnection, false,
                            messageCount, logProvider, generateData,
                            verify, false, scope, false).ConfigureAwait(false);
                        Thread.Sleep(time);
                        var consumer = new ConsumerAsyncShared<FakeMessageA> { Factory = Factory };
                        consumer.RunConsumer<TTransportInit>(queueConnection,
                            false, logProvider,
                            runtime, messageCount,
                            timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35),
                            "second(*%10)", enableChaos, scope, null);
                    }
                    else if (messageType == 3)
                    {
                        var producer = new ProducerAsyncShared();
                        await producer.RunTestAsync<TTransportInit, FakeMessageB>(queueConnection, false,
                            messageCount, logProvider, generateData,
                            verify, false, oCreation.Scope, false).ConfigureAwait(false);
                        Thread.Sleep(time);
                        var consumer = new ConsumerAsyncShared<FakeMessageB> { Factory = Factory };
                        consumer.RunConsumer<TTransportInit>(queueConnection,
                            false, logProvider,
                            runtime, messageCount,
                            timeOut, readerCount,
                            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", enableChaos, scope,
                            null);
                    }

                    verifyQueueCount(queueConnection, oCreation.BaseTransportOptions, scope, 0, false,
                        false);

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

#pragma warning disable xUnit1013 // Public method should be marked as test
        public async Task RunWithFactory<TTransportInit, TTransportCreate>(
            QueueConnection queueConnection,
            int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            int messageType, bool enableChaos,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            Action<QueueConnection, IBaseTransportOptions, ICreationScope, int, bool, bool> verifyQueueCount,
            ITaskFactory factory)
            where TTransportInit : ITransportInit, new()
            where TTransportCreate : class, IQueueCreation
        {
            Factory = factory;
            await Run<TTransportInit, TTransportCreate>(queueConnection, messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType,
                enableChaos, setOptions, generateData, verify, verifyQueueCount).ConfigureAwait(false);
        }


        public static ITaskFactory CreateFactory(int maxThreads, int maxQueueSize, out SchedulerContainer schedulerCreator)
        {
            schedulerCreator = new SchedulerContainer();
            var taskScheduler = schedulerCreator.CreateTaskScheduler();

            taskScheduler.Configuration.MaximumThreads = maxThreads;

            taskScheduler.Start();
            return schedulerCreator.CreateTaskFactory(taskScheduler);
        }
    }
}
