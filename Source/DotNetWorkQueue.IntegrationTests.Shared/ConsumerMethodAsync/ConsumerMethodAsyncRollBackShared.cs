using System;
using System.Threading;
using DotNetWorkQueue.Logging;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync
{
    public class ConsumerMethodAsyncRollBackShared
    {
        public void RunConsumer<TTransportInit>(string queueName,
            string connectionString,
            bool addInterceptors,
            int workerCount,
            ILogProvider logProvider,
            int timeOut,
            int readerCount,
            int queueSize,
            int runTime,
            int messageCount,
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
                        }
                        Assert.Equal(messageCount, MethodIncrementWrapper.Count(id));
                        VerifyMetrics.VerifyProcessedCount(queueName, metrics.GetCurrentMetrics(), messageCount);
                        VerifyMetrics.VerifyRollBackCount(queueName, metrics.GetCurrentMetrics(), messageCount, 1, 0);
                    }
                }
            }
        }
    }
}
