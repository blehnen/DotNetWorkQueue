using System;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation
{
    public class MultiConsumerAsync
    {
        public void Run<TTransportInit, TTransportCreate>(
            string queueName,
            string connectionString,
            int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize, bool enableChaos,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            Action<string, string, IBaseTransportOptions, ICreationScope, int, bool, bool> verifyQueueCount)
            where TTransportInit : ITransportInit, new()
            where TTransportCreate : class, IQueueCreation
        {
            var factory = SimpleConsumerAsync.CreateFactory(workerCount, queueSize, out var schedulerContainer);
            using (schedulerContainer)
            {
                using (factory.Scheduler)
                {
                    var task1 =
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer<TTransportInit, TTransportCreate>(queueName, connectionString, messageCount,
                                    runtime,
                                    timeOut, workerCount, readerCount, queueSize, 1, enableChaos, setOptions,
                                    generateData, verify, verifyQueueCount, factory));

                    var task2 =
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer<TTransportInit, TTransportCreate>(queueName, connectionString, messageCount,
                                    runtime,
                                    timeOut, workerCount, readerCount, queueSize, 2, enableChaos, setOptions,
                                    generateData, verify, verifyQueueCount, factory));

                    var task3 =
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer<TTransportInit, TTransportCreate>(queueName, connectionString, messageCount,
                                    runtime,
                                    timeOut, workerCount, readerCount, queueSize, 3, enableChaos, setOptions,
                                    generateData, verify, verifyQueueCount, factory));

                    Task.WaitAll(task1, task2, task3);
                }
            }
        }

        private void RunConsumer<TTransportInit, TTransportCreate>(
            string queueName,
            string connectionString,
            int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            int messageType, bool enableChaos,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            Action<string, string, IBaseTransportOptions, ICreationScope, int, bool, bool> verifyQueueCount,
            ITaskFactory factory)
            where TTransportInit : ITransportInit, new()
            where TTransportCreate : class, IQueueCreation
        {
            var queue = new SimpleConsumerAsync();
            queue.RunWithFactory<TTransportInit, TTransportCreate>(queueName, connectionString, messageCount, runtime, timeOut, workerCount, readerCount,
                queueSize,
                messageType, enableChaos, setOptions, generateData, verify, verifyQueueCount, factory);
        }
    }
}