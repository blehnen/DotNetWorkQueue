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
using System.Threading;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Metrics.Net;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync
{
    public class ConsumerMethodAsyncErrorShared
    {
        public void RunConsumer<TTransportInit>(string queueName, string connectionString, bool addInterceptors,
            ILogProvider logProvider,
            int messageCount, int workerCount, int timeOut,
            int queueSize, int readerCount,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime,
            Guid id, string updateTime)
            where TTransportInit : ITransportInit, new()
        {

            using (var metrics = new Metrics.Net.Metrics(queueName))
            {
                var addInterceptorConsumer = InterceptorAdding.No;
                if (addInterceptors)
                {
                    addInterceptorConsumer = InterceptorAdding.ConfigurationOnly;
                }

                using (
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics)
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
                            VerifyMetrics.VerifyRollBackCount(queueName, metrics.GetCurrentMetrics(), messageCount, 3, 2);
                    }
                }
            }
        }
    }
}
