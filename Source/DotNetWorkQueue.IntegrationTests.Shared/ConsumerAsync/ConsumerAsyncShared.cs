using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DotNetWorkQueue.Logging;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync
{
    public class ConsumerAsyncShared<TMessage>
        where TMessage : class
    {
        public ITaskFactory Factory { get; set; }

        public
            void RunConsumer<TTransportInit>(string queueName,
                string connectionString,
                bool addInterceptors,
                ILogProvider logProvider,
                int runTime,
                int messageCount,
                int timeOut,
                int readerCount,
                TimeSpan heartBeatTime, 
                TimeSpan heartBeatMonitorTime,
                string updateTime,
                List<string> routes = null)
            where TTransportInit : ITransportInit, new()
        {

            var metricName = queueName;
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
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics)
                    )
                {

                    using (
                        var queue =
                            creator
                                .CreateConsumerQueueScheduler(
                                    queueName, connectionString, Factory))
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
                        VerifyMetrics.VerifyProcessedCount(queueName, metrics.GetCurrentMetrics(), messageCount * routes.Count);
                    }
                    else
                    {
                        Assert.Equal(messageCount, processedCount.ProcessedCount);
                        VerifyMetrics.VerifyProcessedCount(queueName, metrics.GetCurrentMetrics(), messageCount);
                    }
                    LoggerShared.CheckForErrors(queueName);
                }
            }
        }
    }
}
