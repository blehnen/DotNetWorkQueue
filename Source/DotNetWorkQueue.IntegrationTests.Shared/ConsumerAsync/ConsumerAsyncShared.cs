using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync
{
    public class ConsumerAsyncShared<TMessage>
        where TMessage : class
    {
        public ITaskFactory Factory { get; set; }

        public
            void RunConsumer<TTransportInit>(QueueConnection queueConnection,
                bool addInterceptors,
                ILogger logProvider,
                int runTime,
                int messageCount,
                int timeOut,
                int readerCount,
                TimeSpan heartBeatTime, 
                TimeSpan heartBeatMonitorTime,
                string updateTime,
                bool enableChaos,
                List<string> routes = null)
            where TTransportInit : ITransportInit, new()
        {

            if (enableChaos)
                timeOut *= 2;

            var metricName = queueConnection.Queue;
            if (routes != null)
            {
                metricName = routes.Aggregate(metricName, (current, route) => current + route + "|-|");
            }

            using (var metrics = new Metrics.Metrics(metricName))
            {
                var processedCount = new IncrementWrapper();
                var addInterceptorConsumer = InterceptorAdding.No;
                if (addInterceptors)
                {
                    addInterceptorConsumer = InterceptorAdding.ConfigurationOnly;
                }

                using (
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics, false, enableChaos)
                    )
                {

                    using (
                        var queue =
                            creator
                                .CreateConsumerQueueScheduler(
                                    queueConnection, Factory))
                    {
                        SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, readerCount, heartBeatTime,
                            heartBeatMonitorTime, updateTime, null);

                        if(routes != null)
                            queue.Configuration.Routes.AddRange(routes);

                        var waitForFinish = new ManualResetEventSlim(false);
                        waitForFinish.Reset();

                        //start looking for work
                        queue.Start<TMessage>((message, notifications) =>
                        {
                            if (routes != null && routes.Count > 0)
                            {
                                MessageHandlingShared.HandleFakeMessages(message, runTime, processedCount, messageCount * routes.Count,
                                waitForFinish);
                            }
                            else
                            {
                                MessageHandlingShared.HandleFakeMessages(message, runTime, processedCount, messageCount,
                                waitForFinish);
                            }
                        });

                        waitForFinish.Wait(timeOut*1000);
                    }

                    Assert.Null(processedCount.IdError);
                    if (routes != null && routes.Count > 0)
                    {
                        Assert.Equal(messageCount * routes.Count, processedCount.ProcessedCount);
                        VerifyMetrics.VerifyProcessedCount(queueConnection.Queue, metrics.GetCurrentMetrics(), messageCount * routes.Count);
                    }
                    else
                    {
                        Assert.Equal(messageCount, processedCount.ProcessedCount);
                        VerifyMetrics.VerifyProcessedCount(queueConnection.Queue, metrics.GetCurrentMetrics(), messageCount);
                    }
                    LoggerShared.CheckForErrors(queueConnection.Queue);
                }
            }
        }
    }
}
