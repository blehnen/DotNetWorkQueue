﻿using DotNetWorkQueue.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod
{
    public class ConsumerMethodShared
    {
        public void RunConsumer<TTransportInit>(QueueConnection queueConnection, bool addInterceptors,
            ILogger logProvider,
            int runTime, int messageCount,
            int workerCount, int timeOut,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, Guid id, string updateTime, bool enableChaos, ICreationScope scope)
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
                            metrics, false, enableChaos, scope, trace.Source)
                    )
                    {
                        using (
                            var queue =
                            creator.CreateMethodConsumer(queueConnection, x => x.RegisterNonScopedSingleton(scope).RegisterNonScopedSingleton(trace.Source)))
                        {
                            SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, workerCount, heartBeatTime,
                                heartBeatMonitorTime, updateTime, null);
                            queue.Start(CreateNotifications.Create(logProvider));
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
                        VerifyMetrics.VerifyProcessedCount(queueConnection.Queue, metrics.GetCurrentMetrics(),
                            messageCount);
                        LoggerShared.CheckForErrors(queueConnection.Queue);
                    }
                }
            }
        }
    }
}
