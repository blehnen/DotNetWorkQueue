using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using Microsoft.Extensions.Logging;
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
                ICreationScope scope,
                string route,
                Action<QueueConsumerConfiguration> setQueueOptions = null)
            where TTransportInit : ITransportInit, new()
        {

            if (enableChaos)
                timeOut *= 2;

            var metricName = queueConnection.Queue;
            if (!string.IsNullOrEmpty(route))
            {
                metricName = queueConnection.Queue + "|-|" + route;
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
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider,
                        metrics, false, enableChaos, scope)
                )
                {

                    using (
                        var queue =
                        creator
                            .CreateConsumerQueueScheduler(
                                queueConnection, Factory))
                    {
                        SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, readerCount, heartBeatTime,
                            heartBeatMonitorTime, updateTime, route);

                        if (!string.IsNullOrWhiteSpace(route))
                            queue.Configuration.Routes.Add(route);

                        setQueueOptions?.Invoke(queue.Configuration);

                        var waitForFinish = new ManualResetEventSlim(false);
                        waitForFinish.Reset();

                        //start looking for work
                        queue.Start<TMessage>((message, notifications) =>
                        {
                            MessageHandlingShared.HandleFakeMessages(message, runTime, processedCount, messageCount,
                                waitForFinish);
                        });

                        waitForFinish.Wait(timeOut * 1000);
                    }

                    Assert.Null(processedCount.IdError);
                    Assert.Equal(messageCount, processedCount.ProcessedCount);
                    VerifyMetrics.VerifyProcessedCount(queueConnection.Queue, metrics.GetCurrentMetrics(),
                        messageCount);
                    LoggerShared.CheckForErrors(queueConnection.Queue);
                }
            }
        }
    }
}
