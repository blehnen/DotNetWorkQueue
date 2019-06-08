using System;
using System.Threading;
using DotNetWorkQueue.Logging;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync
{
    public class ConsumerMethodAsyncShared
    {
        public ITaskFactory Factory { get; set; }

        public
            void RunConsumer<TTransportInit>(string queueName,
                string connectionString,
                bool addInterceptors,
                ILogProvider logProvider,
                int runTime,
                int messageCount,
                int timeOut,
                int readerCount,
                TimeSpan heartBeatTime, 
                TimeSpan heartBeatMonitorTime,
                Guid id,
                string updateTime,
                bool enableChaos)
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
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics, false, enableChaos)
                    )
                {

                    using (
                        var queue =
                            creator
                                .CreateConsumerMethodQueueScheduler(
                                    queueName, connectionString, Factory))
                    {
                        SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, readerCount, heartBeatTime,
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
