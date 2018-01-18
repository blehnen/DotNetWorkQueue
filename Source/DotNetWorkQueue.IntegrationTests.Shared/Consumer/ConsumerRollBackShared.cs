using System;
using System.Collections.Concurrent;
using System.Threading;
using DotNetWorkQueue.Logging;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.Consumer
{
    public class ConsumerRollBackShared<TMessage>
        where TMessage : class
    {
        public void RunConsumer<TTransportInit>(string queueName,
            string connectionString,
            bool addInterceptors,
            int workerCount,
            ILogProvider logProvider,
            int timeOut,
            int runTime,
            long messageCount,
            TimeSpan heartBeatTime, 
            TimeSpan heartBeatMonitorTime,
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
                var haveIProcessedYouBefore = new ConcurrentDictionary<string, int>();
                using (
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics)
                    )
                {
                    using (
                        var queue =
                            creator.CreateConsumer(queueName,
                                connectionString))
                    {
                        SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, workerCount, heartBeatTime,
                            heartBeatMonitorTime, updateTime, route);
                        var waitForFinish = new ManualResetEventSlim(false);
                        waitForFinish.Reset();

                        //start looking for work
                        queue.Start<TMessage>((message, notifications) =>
                        {
                            MessageHandlingShared.HandleFakeMessagesRollback(message, runTime, processedCount,
                                messageCount,
                                waitForFinish, haveIProcessedYouBefore);
                        });

                        waitForFinish.Wait(timeOut*1000);
                    }
                    Assert.Equal(messageCount, processedCount.ProcessedCount);
                    VerifyMetrics.VerifyProcessedCount(queueName, metrics.GetCurrentMetrics(), messageCount);
                    VerifyMetrics.VerifyRollBackCount(queueName, metrics.GetCurrentMetrics(), messageCount, 1, 0);
                    LoggerShared.CheckForErrors(queueName);
                    haveIProcessedYouBefore.Clear();
                }
            }
        }
    }
}
