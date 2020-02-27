using System;
using System.IO;
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
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, string updateTime, string route, bool enableChaos)
            where TTransportInit : ITransportInit, new()
        {

            if (enableChaos)
                timeOut *= 2;

            using (var metrics = new Metrics.Metrics(queueName))
            {
                var addInterceptorConsumer = InterceptorAdding.No;
                if (addInterceptors)
                {
                    addInterceptorConsumer = InterceptorAdding.ConfigurationOnly;
                }

                var processedCount = new IncrementWrapper();
                using (
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider,
                        metrics, false, enableChaos)
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
                            MessageHandlingShared.HandleFakeMessagesError(processedCount, waitForFinish,
                                messageCount, message);
                        });

                        waitForFinish.Wait(timeOut * 1000);

                        //wait 3 more seconds before starting to shutdown
                        Thread.Sleep(3000);
                    }

                    if (rollBacks)
                        VerifyMetrics.VerifyRollBackCount(queueName, metrics.GetCurrentMetrics(), messageCount, 2, 2);
                }
            }
        }

        public void PurgeErrorMessages<TTransportInit>(string queueName, string connectionString,
            bool addInterceptors, ILogProvider logProvider)
            where TTransportInit : ITransportInit, new()
        {
            using (var metrics = new Metrics.Metrics(queueName))
            {
                var addInterceptorConsumer = InterceptorAdding.No;
                if (addInterceptors)
                {
                    addInterceptorConsumer = InterceptorAdding.ConfigurationOnly;
                }

                using (
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider,
                        metrics, false, false)
                )
                {

                    using (
                        var queue =
                            creator.CreateConsumer(queueName,
                                connectionString))
                    {
                        SharedSetup.SetupDefaultConsumerQueueErrorPurge(queue.Configuration);
                        SharedSetup.SetupDefaultErrorRetry(queue.Configuration);

                        var waitForFinish = new ManualResetEventSlim(false);
                        waitForFinish.Reset();

                        //start looking for work
                        queue.Start<TMessage>((message, notifications) =>
                            throw new Exception("There should have been no data to process"));

                        //wait for 30 seconds
                        waitForFinish.Wait(30000);
                    }
                }
            }
        }
    }
}
