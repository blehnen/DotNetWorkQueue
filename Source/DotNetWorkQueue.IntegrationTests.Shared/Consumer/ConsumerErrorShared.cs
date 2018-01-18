using System;
using System.Threading;
using DotNetWorkQueue.Logging;

namespace DotNetWorkQueue.IntegrationTests.Shared.Consumer
{
    public class ConsumerErrorShared<TMessage>
        where TMessage : class
    {
        public void RunConsumer<TTransportInit>(string queueName, string connectionString, bool addInterceptors,
            ILogProvider logProvider,
            int workerCount, int timeOut, int messageCount,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, string updateTime, string route)
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
                using (
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics)
                    )
                {

                    bool rollBacks;
                    using (
                        var queue =
                            creator.CreateConsumer(queueName,
                                connectionString))
                    {
                        rollBacks = queue.Configuration.TransportConfiguration.MessageRollbackSupported;

                        SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, workerCount, heartBeatTime,
                            heartBeatMonitorTime, updateTime, route);
                        SharedSetup.SetupDefaultErrorRetry(queue.Configuration);

                        var waitForFinish = new ManualResetEventSlim(false);
                        waitForFinish.Reset();

                        //start looking for work
                        queue.Start<TMessage>((message, notifications) =>
                        {
                            MessageHandlingShared.HandleFakeMessagesError(processedCount, waitForFinish, messageCount);
                        });

                        waitForFinish.Wait(timeOut*1000);

                        //wait 3 more seconds before starting to shutdown
                        Thread.Sleep(3000);
                    }

                    if(rollBacks)
                        VerifyMetrics.VerifyRollBackCount(queueName, metrics.GetCurrentMetrics(), messageCount, 3, 2);
                }
            }
        }
    }
}
