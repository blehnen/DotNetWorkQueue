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
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync
{
    public class ConsumerMethodAsyncShared
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
                Guid id)
            where TTransportInit : ITransportInit, new()
        {

            using (var metrics = new Metrics.Net.Metrics(queueName))
            {
                var addInterceptorConsumer = InterceptorAdding.No;
                if (addInterceptors)
                {
                    addInterceptorConsumer = InterceptorAdding.ConfigurationOnly;
                }

                var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics);

                using (
                    var queue =
                        creator
                            .CreateConsumerMethodQueueScheduler(
                                queueName, connectionString, Factory))
                {
                    SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, readerCount, heartBeatTime, heartBeatMonitorTime);
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
                }

                Assert.Equal(messageCount, MethodIncrementWrapper.Count(id));
                VerifyMetrics.VerifyProcessedCount(queueName, metrics.GetCurrentMetrics(), messageCount);
                LoggerShared.CheckForErrors(queueName);
            }
        }
    }
}
