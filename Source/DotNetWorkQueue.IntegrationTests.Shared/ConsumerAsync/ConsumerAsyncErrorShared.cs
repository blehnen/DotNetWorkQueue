using System;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync
{
    public class ConsumerAsyncErrorShared<TMessage>
        where TMessage : class
    {
        public void RunConsumer<TTransportInit>(QueueConnection queueConnection, bool addInterceptors,
            ILogger logProvider,
            int messageCount, int workerCount, int timeOut,
            int queueSize, int readerCount,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime,
            string updateTime,
            string route,
            bool enableChaos)
            where TTransportInit : ITransportInit, new()
        {

            if (enableChaos)
                timeOut *= 2;

            using (var metrics = new Metrics.Metrics(queueConnection.Queue))
            {
                var addInterceptorConsumer = InterceptorAdding.No;
                if (addInterceptors)
                {
                    addInterceptorConsumer = InterceptorAdding.ConfigurationOnly;
                }

                var processedCount = new IncrementWrapper();
                using (
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics, false, enableChaos)
                    )
                {

                    using (var schedulerCreator =
                        new SchedulerContainer(
                            // ReSharper disable once AccessToDisposedClosure
                            serviceRegister => serviceRegister.Register(() => metrics, LifeStyles.Singleton), options => SharedSetup.SetOptions(options, enableChaos)))
                    {
                        bool rollBacks;
                        using (var taskScheduler = schedulerCreator.CreateTaskScheduler())
                        {
                            taskScheduler.Configuration.MaximumThreads = workerCount;
                            taskScheduler.Configuration.MaxQueueSize = queueSize;

                            taskScheduler.Start();
                            var taskFactory = schedulerCreator.CreateTaskFactory(taskScheduler);

                            using (
                                var queue =
                                    creator
                                        .CreateConsumerQueueScheduler(
                                            queueConnection, taskFactory))
                            {
                                rollBacks = queue.Configuration.TransportConfiguration.MessageRollbackSupported;
                                SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, readerCount, heartBeatTime,
                                    heartBeatMonitorTime, updateTime, route);
                                SharedSetup.SetupDefaultErrorRetry(queue.Configuration);

                                var waitForFinish = new ManualResetEventSlim(false);
                                waitForFinish.Reset();

                                //start looking for work
                                queue.Start<TMessage>((message, notifications) =>
                                {
                                    MessageHandlingShared.HandleFakeMessagesError(processedCount, waitForFinish,
                                        messageCount, message);
                                });

                                waitForFinish.Wait(timeOut*1000);

                                //wait for last error to be saved if needed.
                                Thread.Sleep(3000);
                            }
                        }

                        if(rollBacks)
                            VerifyMetrics.VerifyRollBackCount(queueConnection.Queue, metrics.GetCurrentMetrics(), messageCount, 2, 2);
                    }
                }
            }
        }
        public void PurgeErrorMessages<TTransportInit>(QueueConnection queueConnection,
            bool addInterceptors, ILogger logProvider, bool actuallyPurge)
            where TTransportInit : ITransportInit, new()
        {
            using (var metrics = new Metrics.Metrics(queueConnection.Queue))
            {
                var addInterceptorConsumer = InterceptorAdding.No;
                if (addInterceptors)
                {
                    addInterceptorConsumer = InterceptorAdding.ConfigurationOnly;
                }

                var processedCount = new IncrementWrapper();
                using (
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics, false, false)
                    )
                {

                    using (var schedulerCreator =
                        new SchedulerContainer(
                            // ReSharper disable once AccessToDisposedClosure
                            serviceRegister => serviceRegister.Register(() => metrics, LifeStyles.Singleton), options => SharedSetup.SetOptions(options, false)))
                    {
                        using (var taskScheduler = schedulerCreator.CreateTaskScheduler())
                        {
                            taskScheduler.Start();
                            var taskFactory = schedulerCreator.CreateTaskFactory(taskScheduler);

                            using (
                                var queue =
                                    creator
                                        .CreateConsumerQueueScheduler(
                                            queueConnection, taskFactory))
                            {
                              
                                SharedSetup.SetupDefaultConsumerQueueErrorPurge(queue.Configuration, actuallyPurge);
                                SharedSetup.SetupDefaultErrorRetry(queue.Configuration);

                                var waitForFinish = new ManualResetEventSlim(false);
                                waitForFinish.Reset();

                                //start looking for work
                                queue.Start<TMessage>((message, notifications) => throw new Exception("There should have been no data to process"));

                                //wait for 30 seconds
                                waitForFinish.Wait(15000);
                            }
                        }
                    }
                }
            }
        }
    }
}
