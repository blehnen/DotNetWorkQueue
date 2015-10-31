// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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

using System.Threading;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Metrics.Net;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync
{
    public class ConsumerAsyncErrorShared<TMessage>
        where TMessage : class
    {
        public void RunConsumer<TTransportInit>(string queueName, string connectionString, bool addInterceptors,
            ILogProvider logProvider,
            int messageCount, int workerCount, int timeOut,
            int queueSize, int readerCount)
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
                var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics);

                using (var schedulerCreator =
                    new SchedulerContainer(
                        // ReSharper disable once AccessToDisposedClosure
                        serviceRegister => serviceRegister.Register(() => metrics, LifeStyles.Singleton)))
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
                                    .CreateConsumerQueueScheduler(
                                        queueName, connectionString, taskFactory))
                        {
                            SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, readerCount);
                            SharedSetup.SetupDefaultErrorRetry(queue.Configuration);

                            var waitForFinish = new ManualResetEventSlim(false);
                            waitForFinish.Reset();

                            //start looking for work
                            queue.Start<TMessage>(((message, notifications) =>
                            {
                                MessageHandlingShared.HandleFakeMessagesError(processedCount, waitForFinish,
                                    messageCount);
                            }));

                            waitForFinish.Wait(timeOut*1000);

                            //wait for last error to be saved if needed.
                            Thread.Sleep(3000);
                        }
                    }
                    VerifyMetrics.VerifyRollBackCount(queueName, metrics.GetCurrentMetrics(), messageCount, 3, 2);
                }
            }
        }
    }
}
