using System;
using System.Threading;
using DotNetWorkQueue.Logging;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod
{
    public class ConsumerMethodPoisonMessageShared
    {
        public void RunConsumer<TTransportInit>(string queueName,
            string connectionString,
            bool addInterceptors,
            int workerCount,
            ILogProvider logProvider,
            int timeOut,
            long messageCount,
            TimeSpan heartBeatTime, 
            TimeSpan heartBeatMonitorTime,
            string updateTime)
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
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics,
                        true))
                {
                    using (
                        var queue =
                            creator.CreateMethodConsumer(queueName,
                                connectionString))
                    {
                        SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, workerCount, heartBeatTime,
                            heartBeatMonitorTime, updateTime, null);

                        var waitForFinish = new ManualResetEventSlim(false);
                        waitForFinish.Reset();
                        queue.Start();
                        for (var i = 0; i < timeOut; i++)
                        {
                            if (VerifyMetrics.GetPoisonMessageCount(metrics.GetCurrentMetrics()) == messageCount)
                            {
                                break;
                            }
                            Thread.Sleep(1000);
                        }
                    }
                    VerifyMetrics.VerifyPoisonMessageCount(queueName, metrics.GetCurrentMetrics(), messageCount);
                }
            }
        }
    }
}
