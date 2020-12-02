using System;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.Consumer
{
    public class ConsumerShared<TMessage>
         where TMessage : class
    {
        public void RunConsumer<TTransportInit>(QueueConnection queueConnection, bool addInterceptors,
            ILogProvider logProvider,
            int runTime, int messageCount,
            int workerCount, int timeOut,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, string updateTime, bool enableChaos)
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
                            heartBeatMonitorTime, updateTime, null);
                        var waitForFinish = new ManualResetEventSlim(false);
                        waitForFinish.Reset();
                        //start looking for work
                        queue.Start<TMessage>((message, notifications) =>
                        {
                            MessageHandlingShared.HandleFakeMessages(message, runTime, processedCount, messageCount,
                                waitForFinish);
                        });

                        waitForFinish.Wait(timeOut*1000);
                    }

                    Assert.Null(processedCount.IdError);
                    Assert.Equal(messageCount, processedCount.ProcessedCount);
                    VerifyMetrics.VerifyProcessedCount(queueConnection.Queue, metrics.GetCurrentMetrics(), messageCount);
                    LoggerShared.CheckForErrors(queueConnection.Queue);
                }
            }
        }
    }
}
