using DotNetWorkQueue.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync
{
    /// <summary>
    /// The asynchronous twin of <see cref="Consumer.ConsumerExpiredMessageShared{TMessage}"/>.
    ///
    /// Expiring a message is the one path where the consumer removes a message from inside the
    /// de-queue itself, and until #284 the asynchronous consumer did that with a blocking call. There
    /// was no expired-message test on this consumer at all - only on the synchronous one and on
    /// ConsumerMethod - so the removal that runs here reached no test, which is how it stayed
    /// synchronous unnoticed.
    /// </summary>
    public class ConsumerAsyncExpiredMessageShared<TMessage>
        where TMessage : class
    {
        public void RunConsumer<TTransportInit>(QueueConnection queueConnection,
            bool addInterceptors,
            int workerCount,
            ILogger logProvider,
            int timeOut,
            int readerCount,
            int messageCount,
            TimeSpan heartBeatTime,
            TimeSpan heartBeatMonitorTime,
            string updateTime,
            string route,
            bool enableChaos, ICreationScope scope)
            where TTransportInit : ITransportInit, new()
        {
            if (enableChaos)
                timeOut *= 2;

            using (var trace = SharedSetup.CreateTrace("consumer-expired-async"))
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
                        var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider,
                            metrics, false, enableChaos, scope, trace.Source))
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
                                        .CreateConsumerQueueScheduler(queueConnection, taskFactory))
                                {
                                    SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, readerCount,
                                        heartBeatTime, heartBeatMonitorTime, updateTime, route);
                                    queue.Configuration.MessageExpiration.Enabled = true;
                                    queue.Configuration.MessageExpiration.MonitorTime = TimeSpan.FromSeconds(8);

                                    //A real handle rather than null: nothing should reach the handler,
                                    //but if something does it must increment the count and fail the
                                    //assertion below rather than throw here and mask it.
                                    var waitForFinish = new ManualResetEventSlim(false);
                                    waitForFinish.Reset();

                                    //start looking for work
                                    queue.Start<TMessage>((message, notifications) =>
                                    {
                                        MessageHandlingShared.HandleFakeMessages<TMessage>(null, 0, processedCount,
                                            messageCount, waitForFinish);
                                    }, CreateNotifications.Create(logProvider));

                                    for (var i = 0; i < timeOut; i++)
                                    {
                                        if (VerifyMetrics.GetExpiredMessageCount(metrics.GetCurrentMetrics()) == messageCount)
                                        {
                                            break;
                                        }

                                        Thread.Sleep(1000);
                                    }
                                }
                            }
                        }

                        //Nothing may be handed to the consumer: every message expired before it could be
                        //processed, which is the whole point of the run.
                        Assert.AreEqual(0, processedCount.ProcessedCount);
                        VerifyMetrics.VerifyProcessedCount(queueConnection.Queue, metrics, 0);
                        VerifyMetrics.VerifyExpiredMessageCount(queueConnection.Queue, metrics, messageCount);
                        LoggerShared.CheckForErrors(queueConnection.Queue);
                    }
                }
            }
        }
    }
}
