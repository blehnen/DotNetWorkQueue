using System;
using System.Threading;
using DotNetWorkQueue.Logging;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync
{
    public class ConsumerMethodAsyncPoisonMessageShared
    {
        public void RunConsumer<TTransportInit>(string queueName,
            string connectionString,
            bool addInterceptors,
            int workerCount,
            ILogProvider logProvider,
            int timeOut,
            int readerCount,
            int queueSize,
            long messageCount,
            TimeSpan heartBeatTime, 
            TimeSpan heartBeatMonitorTime,
            string updatetime)
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
                                            queueName, connectionString, taskFactory))
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
                        VerifyMetrics.VerifyPoisonMessageCount(queueName, metrics.GetCurrentMetrics(), messageCount);
                    }
                }
            }
        }
    }
}
