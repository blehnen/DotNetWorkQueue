using System;
using System.Threading;
using DotNetWorkQueue.Logging;

namespace DotNetWorkQueue.IntegrationTests.Shared.Consumer
{
    public class ConsumerPoisonMessageShared<TMessage>
        where TMessage : class
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
            string updateTime,
            string route,
            bool enableChaos)
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

                using (
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics,
                        true, enableChaos))
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
                            MessageHandlingShared.HandleFakeMessageNoOp();
                        });

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
