using System;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod
{
    public class ConsumerMethodErrorShared
    {
        public void PurgeErrorMessages<TTransportInit>(QueueConnection queueConnection,
            bool addInterceptors, ILogProvider logProvider, bool actuallyPurge)
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
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics, false, false)
                )
                {
                    using (
                        var queue =
                            creator.CreateMethodConsumer(queueConnection))
                    {
                        SharedSetup.SetupDefaultConsumerQueueErrorPurge(queue.Configuration, actuallyPurge);
                        SharedSetup.SetupDefaultErrorRetry(queue.Configuration);
                        queue.Start();
                        Thread.Sleep(15000);
                    }
                }
            }
        }

        public void RunConsumer<TTransportInit>(QueueConnection queueConnection, bool addInterceptors,
            ILogProvider logProvider,
            int workerCount, int timeOut, int messageCount,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, Guid id, string updateTime, bool enableChaos)
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

                using (
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics, false, enableChaos)
                    )
                {
                    bool rollbacks;
                    using (
                        var queue =
                            creator.CreateMethodConsumer(queueConnection))
                    {
                        SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, workerCount, heartBeatTime,
                            heartBeatMonitorTime, updateTime, null);
                        SharedSetup.SetupDefaultErrorRetry(queue.Configuration);
                        rollbacks = queue.Configuration.TransportConfiguration.MessageRollbackSupported;
                        queue.Start();

                        var counter = 0;
                        while (counter < timeOut)
                        {
                            if (MethodIncrementWrapper.Count(id) >= messageCount*3)
                            {
                                break;
                            }
                            Thread.Sleep(1000);
                            counter++;
                        }

                        //wait 3 more seconds before starting to shutdown
                        Thread.Sleep(3000);
                    }

                    if(rollbacks)
                        VerifyMetrics.VerifyRollBackCount(queueConnection.Queue, metrics.GetCurrentMetrics(), messageCount, 2, 2);
                }
            }
        }
    }
}
