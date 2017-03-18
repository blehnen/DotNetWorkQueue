// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Threading;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Metrics.Net;

namespace DotNetWorkQueue.IntegrationTests.Shared.Consumer
{
    public class ConsumerErrorShared<TMessage>
        where TMessage : class
    {
        public void RunConsumer<TTransportInit>(string queueName, string connectionString, bool addInterceptors,
            ILogProvider logProvider,
            int workerCount, int timeOut, int messageCount,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, string route = null)
            where TTransportInit : ITransportInit, new()
        {

            using (var metrics = new Metrics.Net.Metrics(queueName))
            {
                var addInterceptorConsumer = InterceptorAdding.No;
                if (addInterceptors)
                {
                    addInterceptorConsumer = InterceptorAdding.ConfigurationOnly;
                }

                var processedCount = new IncrementWrapper();
                using (
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics)
                    )
                {

                    using (
                        var queue =
                            creator.CreateConsumer(queueName,
                                connectionString))
                    {
                        SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, workerCount, heartBeatTime,
                            heartBeatMonitorTime, route);
                        SharedSetup.SetupDefaultErrorRetry(queue.Configuration);

                        var waitForFinish = new ManualResetEventSlim(false);
                        waitForFinish.Reset();

                        //start looking for work
                        queue.Start<TMessage>((message, notifications) =>
                        {
                            MessageHandlingShared.HandleFakeMessagesError(processedCount, waitForFinish, messageCount);
                        });

                        waitForFinish.Wait(timeOut*1000);

                        //wait 3 more seconds before starting to shutdown
                        Thread.Sleep(3000);
                    }

                    VerifyMetrics.VerifyRollBackCount(queueName, metrics.GetCurrentMetrics(), messageCount, 3, 2);
                }
            }
        }
    }
}
