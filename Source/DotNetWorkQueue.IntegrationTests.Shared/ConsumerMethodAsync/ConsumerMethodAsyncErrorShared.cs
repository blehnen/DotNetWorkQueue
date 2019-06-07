using System;
using System.Threading;
using DotNetWorkQueue.Logging;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync
{
    public class ConsumerMethodAsyncErrorShared
    {
        public void RunConsumer<TTransportInit>(string queueName, string connectionString, bool addInterceptors,
            ILogProvider logProvider,
            int messageCount, int workerCount, int timeOut,
            int queueSize, int readerCount,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime,
            Guid id, string updateTime, bool enableChaos)
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
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics, enableChaos)
                    )
                {
                    using (var schedulerCreator =
                        new SchedulerContainer(
                            // ReSharper disable once AccessToDisposedClosure
                            serviceRegister => serviceRegister.Register(() => metrics, LifeStyles.Singleton)))
                    {
                        bool rollback;
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
                                SharedSetup.SetupDefaultErrorRetry(queue.Configuration);
                                rollback = queue.Configuration.TransportConfiguration.MessageRollbackSupported;
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
                        }
                        if(rollback)
                            VerifyMetrics.VerifyRollBackCount(queueName, metrics.GetCurrentMetrics(), messageCount, 2, 2);
                    }
                }
            }
        }
    }
}
