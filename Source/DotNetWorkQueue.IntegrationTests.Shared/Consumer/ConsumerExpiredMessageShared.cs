using System;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.Consumer
{
    public class ConsumerExpiredMessageShared<TMessage>
         where TMessage : class
    {
        public void RunConsumer<TTransportInit>(QueueConnection queueConnection, bool addInterceptors,
            ILogProvider logProvider,
            int runTime, int messageCount,
            int workerCount, int timeOut,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, string updateTime,
            string route, bool enableChaos)
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
                    using (
                        var queue =
                            creator.CreateConsumer(queueConnection))
                    {
                        SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, workerCount, heartBeatTime,
                            heartBeatMonitorTime, updateTime, route);
                        queue.Configuration.MessageExpiration.Enabled = true;
                        queue.Configuration.MessageExpiration.MonitorTime = TimeSpan.FromSeconds(8);
                        var waitForFinish = new ManualResetEventSlim(false);
                        waitForFinish.Reset();
                        //start looking for work
                        queue.Start<TMessage>((message, notifications) =>
                        {
                            MessageHandlingShared.HandleFakeMessages<TMessage>(null, runTime, processedCount, messageCount,
                                waitForFinish);
                        });

                        for (var i = 0; i < timeOut; i++)
                        {
                            if (VerifyMetrics.GetExpiredMessageCount(metrics.GetCurrentMetrics()) == messageCount)
                            {
                                break;
                            }
                            Thread.Sleep(1000);
                        }
                    }

                    Assert.Equal(0, processedCount.ProcessedCount);
                    VerifyMetrics.VerifyProcessedCount(queueConnection.Queue, metrics.GetCurrentMetrics(), 0);
                    VerifyMetrics.VerifyExpiredMessageCount(queueConnection.Queue, metrics.GetCurrentMetrics(), messageCount);
                    LoggerShared.CheckForErrors(queueConnection.Queue);
                }
            }
        }
    }
}
