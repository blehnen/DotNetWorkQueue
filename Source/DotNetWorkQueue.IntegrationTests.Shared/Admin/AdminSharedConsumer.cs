using System;
using System.Threading;
using DotNetWorkQueue.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.Admin
{
    public class AdminSharedConsumer<TMessage>
         where TMessage : class
    {
        public void RunConsumer<TTransportInit>(QueueConnection queueConnection, bool addInterceptors,
            ILogger logProvider,
            int runTime, int messageCount,
            int workerCount, int timeOut,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, string updateTime, bool enableChaos,
            ICreationScope scope)
            where TTransportInit : ITransportInit, new()
        {

            using (var trace = SharedSetup.CreateTrace("consumer-admin"))
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

                    var processedCount = new IncrementWrapper();
                    using (
                        var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider,
                            metrics, false, enableChaos, scope, trace.Source)
                    )
                    {
                        using (
                            var queue =
                            creator.CreateConsumer(queueConnection))
                        {
                            SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, workerCount, heartBeatTime,
                                heartBeatMonitorTime, updateTime, null);
                            var waitForFinish = new ManualResetEventSlim(false);
                            waitForFinish.Reset();

                            var admin = creator.CreateAdminFunctions(queueConnection);
                            var count = admin.Count(null);
                            Assert.Equal(messageCount, count);

                            //start looking for work
                            queue.Start<TMessage>((message, notifications) =>
                            {
                                MessageHandlingShared.HandleFakeMessages(message, runTime, processedCount, messageCount,
                                    waitForFinish);
                            });

                            if (messageCount <= workerCount && runTime > 10)
                            {
                                Thread.Sleep(runTime / 2);
                                var working = admin.Count(QueueStatusAdmin.Processing);
                                var waiting = admin.Count(QueueStatusAdmin.Waiting);
                                Assert.Equal(0, waiting);
                                Assert.Equal(messageCount, working);
                            }
                            else if(runTime > 10)
                            {
                                Thread.Sleep(runTime / 2);
                                var working = admin.Count(QueueStatusAdmin.Processing);
                                Assert.Equal(workerCount, working);
                            }

                            waitForFinish.Wait(timeOut * 1000);
                        }

                        Assert.Null(processedCount.IdError);
                        Assert.Equal(messageCount, processedCount.ProcessedCount);
                        VerifyMetrics.VerifyProcessedCount(queueConnection.Queue, metrics.GetCurrentMetrics(),
                            messageCount);
                        LoggerShared.CheckForErrors(queueConnection.Queue);
                    }
                }
            }
        }
    }
}
