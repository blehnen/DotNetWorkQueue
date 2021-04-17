using System;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation
{
    public class MultiConsumerAsync
    {
        public async Task Run<TTransportInit, TTransportCreate>(
            QueueConnection queueConnection,
            int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize, bool enableChaos,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            Action<QueueConnection, IBaseTransportOptions, ICreationScope, int, bool, bool> verifyQueueCount)
            where TTransportInit : ITransportInit, new()
            where TTransportCreate : class, IQueueCreation
        {
            var factory = SimpleConsumerAsync.CreateFactory(workerCount, queueSize, out var schedulerContainer);
            using (schedulerContainer)
            {
                using (factory.Scheduler)
                {
                    var task1 = await
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer<TTransportInit, TTransportCreate>(queueConnection, messageCount,
                                    runtime,
                                    timeOut, workerCount, readerCount, queueSize, 1, enableChaos, setOptions,
                                    generateData, verify, verifyQueueCount, factory)).ConfigureAwait(false);

                    var task2 = await
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer<TTransportInit, TTransportCreate>(queueConnection, messageCount,
                                    runtime,
                                    timeOut, workerCount, readerCount, queueSize, 2, enableChaos, setOptions,
                                    generateData, verify, verifyQueueCount, factory)).ConfigureAwait(false);

                    var task3 = await
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer<TTransportInit, TTransportCreate>(queueConnection, messageCount,
                                    runtime,
                                    timeOut, workerCount, readerCount, queueSize, 3, enableChaos, setOptions,
                                    generateData, verify, verifyQueueCount, factory)).ConfigureAwait(false);

                    Task.WaitAll(task1, task2, task3);
                }
            }
        }

        private async Task RunConsumer<TTransportInit, TTransportCreate>(
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
            var queue = new SimpleConsumerAsync();
            await queue.RunWithFactory<TTransportInit, TTransportCreate>(queueConnection, messageCount, runtime, timeOut, workerCount, readerCount,
                queueSize,
                messageType, enableChaos, setOptions, generateData, verify, verifyQueueCount, factory).ConfigureAwait(false);
        }
    }
}