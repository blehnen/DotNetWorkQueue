using System;
using System.Collections.Concurrent;
using System.Threading;
using DotNetWorkQueue.Logging;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync
{
    public class ConsumerAsyncRollBackShared<TMessage>
        where TMessage : class
    {
        public void RunConsumer<TTransportInit>(string queueName,
            string connectionString,
            bool addInterceptors,
            int workerCount,
            ILogProvider logProvider,
            int timeOut,
            int readerCount,
            int queueSize,
            int runTime,
            int messageCount,
            TimeSpan heartBeatTime, 
            TimeSpan heartBeatMonitorTime,
            string updateTime,
            string route, bool enableChaos)
            where TTransportInit : ITransportInit, new()
        {
            var processedCount = new IncrementWrapper();
            var haveIProcessedYouBefore = new ConcurrentDictionary<string, int>();

            if (enableChaos)
                timeOut *= 2;

            using (var metrics = new Metrics.Metrics(queueName))
            {
                var addInterceptorConsumer = InterceptorAdding.No;
                if (addInterceptors)
                {
                    addInterceptorConsumer = InterceptorAdding.ConfigurationOnly;
                }

                using (
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics, false, enableChaos)
                    )
                {

                    using (var schedulerCreator = new SchedulerContainer())
                    {
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
                                SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, readerCount, heartBeatTime,
                                    heartBeatMonitorTime, updateTime, route);

                                var waitForFinish = new ManualResetEventSlim(false);
                                waitForFinish.Reset();

                                //start looking for work
                                queue.Start<TMessage>((message, notifications) =>
                                {
                                    MessageHandlingShared.HandleFakeMessagesRollback(message, runTime, processedCount,
                                        messageCount, waitForFinish, haveIProcessedYouBefore);
                                });

                                waitForFinish.Wait(timeOut*1000);
                            }
                        }
                        Assert.Equal(messageCount, processedCount.ProcessedCount);
                        VerifyMetrics.VerifyProcessedCount(queueName, metrics.GetCurrentMetrics(), messageCount);
                        VerifyMetrics.VerifyRollBackCount(queueName, metrics.GetCurrentMetrics(), messageCount, 1, 0);
                    }
                }
                haveIProcessedYouBefore.Clear();
            }
        }
    }
}
