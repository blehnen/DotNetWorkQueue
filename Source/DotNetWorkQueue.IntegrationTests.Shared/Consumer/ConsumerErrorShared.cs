using System;
using System.IO;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;

namespace DotNetWorkQueue.IntegrationTests.Shared.Consumer
{
    public class ConsumerErrorShared<TMessage>
        where TMessage : class
    {
        public void RunConsumer<TTransportInit>(QueueConnection queueConnection, bool addInterceptors,
            ILogger logProvider,
            int workerCount, int timeOut, int messageCount,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, string updateTime, string route, bool enableChaos, ICreationScope scope)
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
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider,
                        metrics, false, enableChaos, scope)
                )
                {

                    bool rollBacks;
                    using (
                        var queue =
                            creator.CreateConsumer(queueConnection))
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
                            MessageHandlingShared.HandleFakeMessagesError(processedCount, waitForFinish,
                                messageCount, message);
                        });

                        waitForFinish.Wait(timeOut * 1000);

                        //wait 3 more seconds before starting to shutdown
                        Thread.Sleep(3000);
                    }

                    if (rollBacks)
                        VerifyMetrics.VerifyRollBackCount(queueConnection.Queue, metrics.GetCurrentMetrics(), messageCount, 2, 2);
                }
            }
        }

        public void PurgeErrorMessages<TTransportInit>(QueueConnection queueConnection,
            bool addInterceptors, ILogger logProvider, bool actuallyPurge, ICreationScope scope)
            where TTransportInit : ITransportInit, new()
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
                        metrics, false, false, scope)
                )
                {

                    using (
                        var queue =
                            creator.CreateConsumer(queueConnection))
                    {
                        SharedSetup.SetupDefaultConsumerQueueErrorPurge(queue.Configuration, actuallyPurge);
                        SharedSetup.SetupDefaultErrorRetry(queue.Configuration);

                        var waitForFinish = new ManualResetEventSlim(false);
                        waitForFinish.Reset();

                        //start looking for work
                        queue.Start<TMessage>((message, notifications) =>
                            throw new Exception("There should have been no data to process"));

                        //wait for 30 seconds
                        waitForFinish.Wait(15000);
                    }
                }
            }
        }
    }
}
