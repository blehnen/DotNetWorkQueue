using System;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod
{
    public class ConsumerMethodExpiredMessageShared
    {
        public void RunConsumer<TTransportInit>(QueueConnection queueConnection, bool addInterceptors,
            ILogger logProvider,
            int runTime, int messageCount,
            int workerCount, int timeOut,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime,
            string updateTime,
            Guid id, bool enableChaos, ICreationScope scope)
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
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics, false, enableChaos, scope)
                    )
                {
                    using (
                        var queue =
                            creator.CreateMethodConsumer(queueConnection, x => x.RegisterNonScopedSingleton(scope)))
                    {
                        SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, workerCount, heartBeatTime,
                            heartBeatMonitorTime, updateTime, null);
                        queue.Configuration.MessageExpiration.Enabled = true;
                        queue.Configuration.MessageExpiration.MonitorTime = TimeSpan.FromSeconds(8);
                        queue.Start();
                        for (var i = 0; i < timeOut; i++)
                        {
                            if (VerifyMetrics.GetExpiredMessageCount(metrics.GetCurrentMetrics()) == messageCount)
                            {
                                break;
                            }
                            Thread.Sleep(1000);
                        }
                    }

                    Assert.Equal(0, MethodIncrementWrapper.Count(id));
                    VerifyMetrics.VerifyProcessedCount(queueConnection.Queue, metrics.GetCurrentMetrics(), 0);
                    VerifyMetrics.VerifyExpiredMessageCount(queueConnection.Queue, metrics.GetCurrentMetrics(), messageCount);
                    LoggerShared.CheckForErrors(queueConnection.Queue);
                }
            }
        }
    }
}
