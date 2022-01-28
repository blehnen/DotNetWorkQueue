using System;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync
{
    public class ConsumerMethodAsyncShared
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
                Guid id,
                string updateTime,
                bool enableChaos, ICreationScope scope)
            where TTransportInit : ITransportInit, new()
        {

            if (enableChaos)
                timeOut *= 2;

            using (var trace = SharedSetup.CreateTrace("consumer"))
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

                        using (
                            var queue =
                            creator
                                .CreateConsumerMethodQueueScheduler(
                                    queueConnection, Factory))
                        {
                            SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, readerCount, heartBeatTime,
                                heartBeatMonitorTime, updateTime, null);
                            queue.Start();
                            var counter = 0;
                            while (counter < timeOut)
                            {
                                if (MethodIncrementWrapper.Count(id) >= messageCount)
                                {
                                    break;
                                }

                                Thread.Sleep(1000);
                                counter++;
                            }
                        }

                        Assert.Equal(messageCount, MethodIncrementWrapper.Count(id));
                        VerifyMetrics.VerifyProcessedCount(queueConnection.Queue, metrics.GetCurrentMetrics(),
                            messageCount);
                        LoggerShared.CheckForErrors(queueConnection.Queue);
                    }
                }
            }
        }
    }
}
