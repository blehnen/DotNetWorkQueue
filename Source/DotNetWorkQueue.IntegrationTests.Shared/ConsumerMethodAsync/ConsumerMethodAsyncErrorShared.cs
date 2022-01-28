﻿using System;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync
{
    public class ConsumerMethodAsyncErrorShared
    {
        public void PurgeErrorMessages<TTransportInit>(QueueConnection queueConnection,
            bool addInterceptors, ILogger logProvider, bool actuallyPurge, ICreationScope scope)
            where TTransportInit : ITransportInit, new()
        {
            using (var trace = SharedSetup.CreateTrace("consumer-error"))
            {
                using (var metrics = new Metrics.Metrics(queueConnection.Queue))
                {
                    var addInterceptorConsumer = InterceptorAdding.No;
                    if (addInterceptors)
                    {
                        addInterceptorConsumer = InterceptorAdding.ConfigurationOnly;
                    }

                    using (
                        var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider,
                            metrics, false, false, scope, trace.Source)
                    )
                    {
                        using (var schedulerCreator =
                               new SchedulerContainer(
                                   // ReSharper disable once AccessToDisposedClosure
                                   serviceRegister => serviceRegister.Register(() => metrics, LifeStyles.Singleton).RegisterNonScopedSingleton(trace.Source)))
                        {
                            using (var taskScheduler = schedulerCreator.CreateTaskScheduler())
                            {
                                taskScheduler.Start();
                                var taskFactory = schedulerCreator.CreateTaskFactory(taskScheduler);

                                using (
                                    var queue =
                                    creator
                                        .CreateConsumerMethodQueueScheduler(
                                            queueConnection, taskFactory))
                                {
                                    SharedSetup.SetupDefaultConsumerQueueErrorPurge(queue.Configuration, actuallyPurge);
                                    SharedSetup.SetupDefaultErrorRetry(queue.Configuration);
                                    queue.Start();
                                    Thread.Sleep(15000);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void RunConsumer<TTransportInit>(QueueConnection queueConnection, bool addInterceptors,
            ILogger logProvider,
            int messageCount, int workerCount, int timeOut,
            int queueSize, int readerCount,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime,
            Guid id, string updateTime, bool enableChaos, ICreationScope scope)
            where TTransportInit : ITransportInit, new()
        {

            if (enableChaos)
                timeOut *= 2;

            using (var trace = SharedSetup.CreateTrace("consumer-error"))
            {
                using (var metrics = new Metrics.Metrics(queueConnection.Queue))
                {
                    var addInterceptorConsumer = InterceptorAdding.No;
                    if (addInterceptors)
                    {
                        addInterceptorConsumer = InterceptorAdding.ConfigurationOnly;
                    }

                    using (
                        var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider,
                            metrics, false, enableChaos, scope, trace.Source)
                    )
                    {
                        using (var schedulerCreator =
                               new SchedulerContainer(
                                   // ReSharper disable once AccessToDisposedClosure
                                   serviceRegister => serviceRegister.Register(() => metrics, LifeStyles.Singleton).RegisterNonScopedSingleton(trace.Source)))
                        {
                            bool rollback;
                            using (var taskScheduler = schedulerCreator.CreateTaskScheduler())
                            {
                                taskScheduler.Configuration.MaximumThreads = workerCount;

                                taskScheduler.Start();
                                var taskFactory = schedulerCreator.CreateTaskFactory(taskScheduler);

                                using (
                                    var queue =
                                    creator
                                        .CreateConsumerMethodQueueScheduler(
                                            queueConnection, taskFactory))
                                {
                                    SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, readerCount,
                                        heartBeatTime,
                                        heartBeatMonitorTime, updateTime, null);
                                    SharedSetup.SetupDefaultErrorRetry(queue.Configuration);
                                    rollback = queue.Configuration.TransportConfiguration.MessageRollbackSupported;
                                    queue.Start();
                                    var counter = 0;
                                    while (counter < timeOut)
                                    {
                                        if (MethodIncrementWrapper.Count(id) >= messageCount * 3)
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

                            if (rollback)
                                VerifyMetrics.VerifyRollBackCount(queueConnection.Queue, metrics.GetCurrentMetrics(),
                                    messageCount, 2, 2);
                        }
                    }
                }
            }
        }
    }
}
