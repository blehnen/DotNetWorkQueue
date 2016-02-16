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

namespace DotNetWorkQueue.IntegrationTests.Shared.Consumer
{
    public class ConsumerCancelWorkShared<TTransportInit, TMessage>
        where TTransportInit : ITransportInit, new()
         where TMessage : class
    {
        private string _queueName;
        private string _connectionString;
        private int _workerCount;
        private TimeSpan _heartBeatTime;
        private TimeSpan _heartBeatMonitorTime;
        private int _runTime;
        private IConsumerQueue _queue;
        private Action<IContainer> _badQueueAdditions;

        public void RunConsumer(string queueName, string connectionString, bool addInterceptors,
            ILogProvider logProvider,
            int runTime, int messageCount,
            int workerCount, int timeOut, Action<IContainer> badQueueAdditions,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime)
        {
            _queueName = queueName;
            _connectionString = connectionString;
            _workerCount = workerCount;
            _runTime = runTime;
            _badQueueAdditions = badQueueAdditions;

            _heartBeatTime = heartBeatTime;
            _heartBeatMonitorTime = heartBeatMonitorTime;

            _queue = CreateConsumerInternalThread();
            var t = new Thread(RunBadQueue);
            t.Start();

            //run consumer
            RunConsumerInternal(queueName, connectionString, addInterceptors, logProvider, runTime,
                messageCount, workerCount, timeOut, _queue, heartBeatTime, heartBeatMonitorTime);
        }


        private void RunConsumerInternal(string queueName, string connectionString, bool addInterceptors,
            ILogProvider logProvider,
            int runTime, int messageCount,
            int workerCount, int timeOut, IDisposable queueBad,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime)
        {

            using (var metrics = new Metrics.Net.Metrics(queueName))
            {
                var processedCount = new IncrementWrapper();
                var addInterceptorConsumer = InterceptorAdding.No;
                if (addInterceptors)
                {
                    addInterceptorConsumer = InterceptorAdding.ConfigurationOnly; 
                }
                var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics);

                using (
                    var queue =
                        creator.CreateConsumer(queueName,
                            connectionString))
                {
                    SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, workerCount, heartBeatTime, heartBeatMonitorTime);
                    var waitForFinish = new ManualResetEventSlim(false);
                    waitForFinish.Reset();
                    //start looking for work
                    queue.Start<TMessage>((message, notifications) =>
                    {
                        MessageHandlingShared.HandleFakeMessages(runTime, processedCount, messageCount,
                            waitForFinish);
                    });

                    var time = runTime*1000/2;
                    waitForFinish.Wait(time);

                    queueBad.Dispose();

                    waitForFinish.Wait(timeOut*1000 - time);
                }

                Assert.Equal(messageCount, processedCount.ProcessedCount);
                VerifyMetrics.VerifyProcessedCount(queueName, metrics.GetCurrentMetrics(), messageCount);
                LoggerShared.CheckForErrors(queueName);
            }
        }

        private IConsumerQueue CreateConsumerInternalThread()
        {
            var creator = SharedSetup.CreateCreator<TTransportInit>(_badQueueAdditions);

            var queue =
                creator.CreateConsumer(_queueName,
                    _connectionString);
 
                SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, _workerCount, _heartBeatTime, _heartBeatMonitorTime);
                return queue;        
        }

        private void RunBadQueue()
        {
            //start looking for work
            _queue.Start<TMessage>((message, notifications) =>
            {
                MessageHandlingShared.HandleFakeMessagesThreadAbort(_runTime * 1000 / 2);
            });
        }
    }
}
