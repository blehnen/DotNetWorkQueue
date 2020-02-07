using System;
using System.Threading;
using DotNetWorkQueue.Logging;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod
{
    public class ConsumerMethodRollBackShared
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
            Guid id, bool enableChaos)
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
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics, false, enableChaos)
                    )
                {
                    using (
                        var queue =
                            creator.CreateMethodConsumer(queueName,
                                connectionString))
                    {
                        SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, workerCount, heartBeatTime,
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

                        //wait for queues to commit records
                        Thread.Sleep(3000);
                    }
                    Assert.Equal(messageCount, MethodIncrementWrapper.Count(id));
                    VerifyMetrics.VerifyProcessedCount(queueName, metrics.GetCurrentMetrics(), messageCount);
                    VerifyMetrics.VerifyRollBackCount(queueName, metrics.GetCurrentMetrics(), messageCount, 1, 0);
                    LoggerShared.CheckForErrors(queueName);
                }
            }
        }
    }
}
