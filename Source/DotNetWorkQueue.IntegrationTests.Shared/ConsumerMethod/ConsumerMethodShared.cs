using System;
using System.Threading;
using DotNetWorkQueue.Logging;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod
{
    public class ConsumerMethodShared
    {
        public void RunConsumer<TTransportInit>(string queueName, string connectionString, bool addInterceptors,
            ILogProvider logProvider,
            int runTime, int messageCount,
            int workerCount, int timeOut,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, Guid id, string updateTime)
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
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics)
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
                    }

                    Assert.Equal(messageCount, MethodIncrementWrapper.Count(id));
                    VerifyMetrics.VerifyProcessedCount(queueName, metrics.GetCurrentMetrics(), messageCount);
                    LoggerShared.CheckForErrors(queueName);
                }
            }
        }
    }
}
