﻿using System;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod
{
    public class ConsumerMethodPoisonMessageShared
    {
        public void RunConsumer<TTransportInit>(QueueConnection queueConnection,
            bool addInterceptors,
            int workerCount,
            ILogger logProvider,
            int timeOut,
            long messageCount,
            TimeSpan heartBeatTime,
            TimeSpan heartBeatMonitorTime,
            string updateTime,
            bool enableChaos, ICreationScope scope)
            where TTransportInit : ITransportInit, new()
        {
            if (enableChaos)
                timeOut *= 2;

            using (var trace = SharedSetup.CreateTrace("consumer-poison"))
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
                            metrics,
                            true, enableChaos, scope, trace.Source))
                    {
                        using (
                            var queue =
                            creator.CreateMethodConsumer(queueConnection, x => x.RegisterNonScopedSingleton(scope).RegisterNonScopedSingleton(trace.Source)))
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

                        VerifyMetrics.VerifyPoisonMessageCount(queueConnection.Queue, metrics.GetCurrentMetrics(),
                            messageCount);
                    }
                }
            }
        }
    }
}
