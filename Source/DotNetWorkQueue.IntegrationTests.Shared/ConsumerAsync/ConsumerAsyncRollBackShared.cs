using System;
using System.Collections.Concurrent;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync
{
    public class ConsumerAsyncRollBackShared<TMessage>
        where TMessage : class
    {
        public void RunConsumer<TTransportInit>(QueueConnection queueConnection,
            bool addInterceptors,
            int workerCount,
            ILogger logProvider,
            int timeOut,
            int readerCount,
            int queueSize,
            int runTime,
            int messageCount,
            TimeSpan heartBeatTime, 
            TimeSpan heartBeatMonitorTime,
            string updateTime,
            string route, bool enableChaos, ICreationScope scope)
            where TTransportInit : ITransportInit, new()
        {
            var processedCount = new IncrementWrapper();
            var haveIProcessedYouBefore = new ConcurrentDictionary<string, int>();

            if (enableChaos)
                timeOut *= 2;

            using (var trace = SharedSetup.CreateTrace("consumer-rollback"))
            {
                using (var metrics = new Metrics.Metrics(queueConnection.Queue))
                {
                    var addInterceptorConsumer = InterceptorAdding.No;
                    if (addInterceptors)
                    {
                        addInterceptorConsumer = InterceptorAdding.ConfigurationOnly;
                    }

                    using (
                        var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider,
                            metrics, false, enableChaos, scope, trace.Source)
                    )
                    {

                        using (var schedulerCreator = new SchedulerContainer((x) => x.RegisterNonScopedSingleton(trace.Source)))
                        {
                            using (var taskScheduler = schedulerCreator.CreateTaskScheduler())
                            {
                                taskScheduler.Configuration.MaximumThreads = workerCount;

                                taskScheduler.Start();
                                var taskFactory = schedulerCreator.CreateTaskFactory(taskScheduler);

                                using (
                                    var queue =
                                    creator
                                        .CreateConsumerQueueScheduler(
                                            queueConnection, taskFactory))
                                {
                                    SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, readerCount,
                                        heartBeatTime,
                                        heartBeatMonitorTime, updateTime, route);

                                    var waitForFinish = new ManualResetEventSlim(false);
                                    waitForFinish.Reset();

                                    //start looking for work
                                    queue.Start<TMessage>((message, notifications) =>
                                    {
                                        MessageHandlingShared.HandleFakeMessagesRollback(message, runTime,
                                            processedCount,
                                            messageCount, waitForFinish, haveIProcessedYouBefore);
                                    });

                                    waitForFinish.Wait(timeOut * 1000);
                                }
                            }

                            Assert.Equal(messageCount, processedCount.ProcessedCount);
                            VerifyMetrics.VerifyProcessedCount(queueConnection.Queue, metrics.GetCurrentMetrics(),
                                messageCount);
                            VerifyMetrics.VerifyRollBackCount(queueConnection.Queue, metrics.GetCurrentMetrics(),
                                messageCount, 1, 0);
                        }
                    }

                    haveIProcessedYouBefore.Clear();
                }
            }
        }
    }
}
