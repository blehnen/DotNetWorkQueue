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
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod
{
    public class ConsumerMethodCancelWorkShared<TTransportInit>
        where TTransportInit : ITransportInit, new()
    {
        private string _queueName;
        private string _connectionString;
        private int _workerCount;
        private TimeSpan _heartBeatTime;
        private TimeSpan _heartBeatMonitorTime;
        private IConsumerMethodQueue _queue;
        private QueueContainer<TTransportInit> _badQueueContainer;
        private Action<IContainer> _badQueueAdditions;

        public void RunConsumer(string queueName, string connectionString, bool addInterceptors,
            ILogProvider logProvider,
            int runTime, int messageCount,
            int workerCount, int timeOut, Action<IContainer> badQueueAdditions,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, Guid id)
        {
            _queueName = queueName;
            _connectionString = connectionString;
            _workerCount = workerCount;
            _badQueueAdditions = badQueueAdditions;

            _heartBeatTime = heartBeatTime;
            _heartBeatMonitorTime = heartBeatMonitorTime;

            _queue = CreateConsumerInternalThread();
            var t = new Thread(RunBadQueue);
            t.Start();

            //run consumer
            RunConsumerInternal(queueName, connectionString, addInterceptors, logProvider, runTime,
                messageCount, workerCount, timeOut, _queue, heartBeatTime, heartBeatMonitorTime, id);
        }


        private void RunConsumerInternal(string queueName, string connectionString, bool addInterceptors,
            ILogProvider logProvider,
            int runTime, int messageCount,
            int workerCount, int timeOut, IDisposable queueBad,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, Guid id)
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

                    using (
                        var queue =
                            creator.CreateMethodConsumer(queueName,
                                connectionString))
                    {
                        SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, workerCount, heartBeatTime,
                            heartBeatMonitorTime);
                        queue.Start();

                        var time = runTime*1000/2;
                        Thread.Sleep(time);
                        queueBad.Dispose();
                        _badQueueContainer.Dispose();

                        var counter = 0;
                        var counterLess = timeOut/2;
                        while (counter < counterLess)
                        {
                            if (MethodIncrementWrapper.Count(id) >= messageCount)
                            {
                                break;
                            }
                            Thread.Sleep(1000);
                            counter++;
                        }

                        //wait for commits in transport...
                        Thread.Sleep(3000);
                    }

                    var count = MethodIncrementWrapper.Count(id);
                    Assert.Equal(messageCount, count);
                    VerifyMetrics.VerifyProcessedCount(queueName, metrics.GetCurrentMetrics(), messageCount);
                    LoggerShared.CheckForErrors(queueName);
                }
            }
        }

        private IConsumerMethodQueue CreateConsumerInternalThread()
        {
            _badQueueContainer = SharedSetup.CreateCreator<TTransportInit>(_badQueueAdditions);

            var queue =
                _badQueueContainer.CreateMethodConsumer(_queueName,
                    _connectionString);
 
                SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, _workerCount, _heartBeatTime, _heartBeatMonitorTime);
                return queue;        
        }

        private void RunBadQueue()
        {
            //start looking for work
            _queue.Start();
        }
    }
}
