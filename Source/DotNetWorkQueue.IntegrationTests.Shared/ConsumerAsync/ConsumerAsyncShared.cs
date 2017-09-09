// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Metrics.Net;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync
{
    public class ConsumerAsyncShared<TMessage>
        where TMessage : class
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
                string updateTime,
                List<string> routes = null)
            where TTransportInit : ITransportInit, new()
        {

            var metricName = queueName;
            if (routes != null)
            {
                metricName = routes.Aggregate(metricName, (current, route) => current + route + "|-|");
            }

            using (var metrics = new Metrics.Net.Metrics(metricName))
            {
                var processedCount = new IncrementWrapper();
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
                            creator
                                .CreateConsumerQueueScheduler(
                                    queueName, connectionString, Factory))
                    {
                        SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, readerCount, heartBeatTime,
                            heartBeatMonitorTime, updateTime, null);

                        if(routes != null)
                            queue.Configuration.Routes.AddRange(routes);

                        var waitForFinish = new ManualResetEventSlim(false);
                        waitForFinish.Reset();

                        //start looking for work
                        queue.Start<TMessage>((message, notifications) =>
                        {
                            if (routes != null && routes.Count > 0)
                            {
                                MessageHandlingShared.HandleFakeMessages(message, runTime, processedCount, messageCount * routes.Count,
                                waitForFinish);
                            }
                            else
                            {
                                MessageHandlingShared.HandleFakeMessages(message, runTime, processedCount, messageCount,
                                waitForFinish);
                            }
                        });

                        waitForFinish.Wait(timeOut*1000);
                    }

                    Assert.Null(processedCount.IdError);
                    if (routes != null && routes.Count > 0)
                    {
                        Assert.Equal(messageCount * routes.Count, processedCount.ProcessedCount);
                        VerifyMetrics.VerifyProcessedCount(queueName, metrics.GetCurrentMetrics(), messageCount * routes.Count);
                    }
                    else
                    {
                        Assert.Equal(messageCount, processedCount.ProcessedCount);
                        VerifyMetrics.VerifyProcessedCount(queueName, metrics.GetCurrentMetrics(), messageCount);
                    }
                    LoggerShared.CheckForErrors(queueName);
                }
            }
        }
    }
}
