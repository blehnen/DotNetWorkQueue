using System;
using System.Threading;
using DotNetWorkQueue.Logging;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync
{
    public class ConsumerAsyncErrorShared<TMessage>
        where TMessage : class
    {
        public void RunConsumer<TTransportInit>(string queueName, string connectionString, bool addInterceptors,
            ILogProvider logProvider,
            int messageCount, int workerCount, int timeOut,
            int queueSize, int readerCount,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime,
            string updateTime,
            string route)
            where TTransportInit : ITransportInit, new()
        {

            using (var metrics = new Metrics.Metrics(queueName))
            {
                var addInterceptorConsumer = InterceptorAdding.No;
                if (addInterceptors)
                {
                    addInterceptorConsumer = InterceptorAdding.ConfigurationOnly;
                }

                var processedCount = new IncrementWrapper();
                using (
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics)
                    )
                {

                    using (var schedulerCreator =
                        new SchedulerContainer(
                            // ReSharper disable once AccessToDisposedClosure
                            serviceRegister => serviceRegister.Register(() => metrics, LifeStyles.Singleton)))
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
                                            queueName, connectionString, taskFactory))
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
                                        messageCount);
                                });

                                waitForFinish.Wait(timeOut*1000);

                                //wait for last error to be saved if needed.
                                Thread.Sleep(3000);
                            }
                        }

                        if(rollBacks)
                            VerifyMetrics.VerifyRollBackCount(queueName, metrics.GetCurrentMetrics(), messageCount, 2, 2);
                    }
                }
            }
        }
    }
}
