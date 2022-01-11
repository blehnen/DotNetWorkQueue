using System;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync
{
    public class ConsumerMethodAsyncPoisonMessageShared
    {
        public void RunConsumer<TTransportInit>(QueueConnection queueConnection,
            bool addInterceptors,
            int workerCount,
            ILogger logProvider,
            int timeOut,
            int readerCount,
            int queueSize,
            long messageCount,
            TimeSpan heartBeatTime, 
            TimeSpan heartBeatMonitorTime,
            string updatetime,
            bool enableChaos, ICreationScope scope)
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
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics,
                        true, enableChaos, scope))
                {
                    using (var schedulerCreator = new SchedulerContainer())
                    {
                        using (var taskScheduler = schedulerCreator.CreateTaskScheduler())
                        {
                            taskScheduler.Configuration.MaximumThreads = workerCount;
                            taskScheduler.Configuration.MaxQueueSize = queueSize;

                            taskScheduler.Start();
                            var taskFactory = schedulerCreator.CreateTaskFactory(taskScheduler);

                            using (
                                var queue =
                                    creator
                                        .CreateConsumerMethodQueueScheduler(
                                            queueConnection, taskFactory))
                            {
                                SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, readerCount, heartBeatTime,
                                    heartBeatMonitorTime, updatetime, null);
                                queue.Start();
                                for (var i = 0; i < timeOut; i++)
                                {
                                    if (VerifyMetrics.GetPoisonMessageCount(metrics.GetCurrentMetrics()) == messageCount)
                                    {
                                        break;
                                    }
                                    Thread.Sleep(1000);
                                }

                                //wait for last error to be saved if needed.
                                Thread.Sleep(3000);
                            }
                        }
                        VerifyMetrics.VerifyPoisonMessageCount(queueConnection.Queue, metrics.GetCurrentMetrics(), messageCount);
                    }
                }
            }
        }
    }
}
