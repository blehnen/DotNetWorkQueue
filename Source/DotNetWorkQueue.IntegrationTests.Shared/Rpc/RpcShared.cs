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
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Metrics.Net;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.Rpc
{
    public class RpcShared<TTransportInit, TTResponse, TTMessage, TTConnectionSettings>
        where TTransportInit : ITransportInit, new()
        where TTResponse: class, new()
        where TTMessage : class, new()
        where TTConnectionSettings : BaseRpcConnection
    {
        private readonly object _createQueue = new object();
        private ConcurrentDictionary<IConnectionInformation, IProducerQueueRpc<TTResponse>> _queues;

        private QueueContainer<TTransportInit> _creator;

        public void Run(string queueNameReceive, string queueNameSend, string connectionStringReceive, string connectionStringSend, ILogProvider logProviderReceive, ILogProvider logProviderSend,
            int runtime, int messageCount, int workerCount, int timeOut, bool async, TTConnectionSettings rpcConnection,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, string updateTime)
        {
            using(_creator = new QueueContainer<TTransportInit>())
            {
                _queues = new ConcurrentDictionary<IConnectionInformation, IProducerQueueRpc<TTResponse>>();
                var processedCount = new IncrementWrapper();
                var waitForFinish = new ManualResetEventSlim(false);

                var task1 = Task.Factory.StartNew(() =>
                    RunRpcReceive(queueNameSend, connectionStringSend, logProviderSend,
                        runtime, processedCount, messageCount,
                        waitForFinish, workerCount, timeOut, heartBeatTime, heartBeatMonitorTime, updateTime));

                RunRpcSend(logProviderSend, messageCount, async, rpcConnection);

                Task.WaitAll(task1);
                LoggerShared.CheckForErrors(queueNameSend);
                LoggerShared.CheckForErrors(queueNameReceive);

                foreach (var queue in _queues)
                {
                    queue.Value.Dispose();
                }
                _queues.Clear();
            }
        }
        private void RunRpcSend(ILogProvider logProvider, int messageCount, bool async, TTConnectionSettings rpcConnection)
        {
            using (
                var creatorRpc =
                    new QueueContainer<TTransportInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                //create the queue
                using (var
                    queue =
                        creatorRpc
                            .CreateRpc
                            <TTResponse, TTMessage, TTConnectionSettings>(rpcConnection))
                {

                    queue.Configuration.TransportConfigurationReceive.QueueDelayBehavior.Clear();
                    queue.Configuration.TransportConfigurationReceive.QueueDelayBehavior.Add(
                        TimeSpan.FromMilliseconds(500));
                    queue.Configuration.MessageExpiration.MonitorTime = TimeSpan.FromSeconds(10);
                    queue.Start();

                    if (async)
                    {
                        SendMultipleMessagesAsync(queue, messageCount);
                    }
                    else
                    {
                        SendMultipleMessages(queue, messageCount);
                    }
                }
            }
        }

        private static void SendMultipleMessages(IRpcQueue<TTResponse, TTMessage> queue, int number)
        {
            //send a bunch of messages
            var numberOfJobs = number;
            var jobs = Enumerable.Range(0, numberOfJobs)
                   .Select(i => new TTMessage());
            Parallel.ForEach(jobs, job =>
            {
                try
                {
                    var message = queue.Send(job, TimeSpan.FromSeconds(60));
                    if (message == null)
                    {
                        throw new DotNetWorkQueueException("The response timed out");
                    }
                    if (message.Body == null)
                    { //RPC call failed
                        //do we have an exception?
                        var error =
                            message.GetHeader(queue.Configuration.HeaderNames.StandardHeaders.RpcConsumerException);
                        if (error != null)
                        {
                            throw new DotNetWorkQueueException("The consumer encountered an error trying to process our request");
                        }
                        throw new DotNetWorkQueueException("A null reply was received, but no error information was found. Examine the log to see if additional information can be found");
                    }
                }
                catch (TimeoutException)
                {
                    throw new DotNetWorkQueueException("The request has timed out");
                }
            });
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private static async void SendMultipleMessagesAsync(IRpcQueue<TTResponse, TTMessage> queue, int number)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            //send a bunch of messages
            var numberOfJobs = number;
            var jobs = Enumerable.Range(0, numberOfJobs)
                   .Select(i => new TTMessage());
            Parallel.ForEach(jobs, async job =>
            {
                try
                {
                    var message = await queue.SendAsync(job, TimeSpan.FromSeconds(60)).ConfigureAwait(false);
                    if (message == null)
                    {
                        throw new DotNetWorkQueueException("The response timed out");
                    }
                    if (message.Body == null)
                    { //RPC call failed
                        //do we have an exception?
                        var error =
                            message.GetHeader(queue.Configuration.HeaderNames.StandardHeaders.RpcConsumerException);
                        if (error != null)
                        {
                            throw new DotNetWorkQueueException("The consumer encountered an error trying to process our request");
                        }
                        throw new DotNetWorkQueueException("A null reply was received, but no error information was found. Examine the log to see if additional information can be found");
                    }
                }
                catch (TimeoutException)
                {
                    throw new DotNetWorkQueueException("The request has timed out");
                }
            });
        }

        private void RunRpcReceive(string queueName, string connectionString,
            ILogProvider logProvider,
            int runTime, IncrementWrapper processedCount, int messageCount, ManualResetEventSlim waitForFinish,
            int workerCount, int timeOut,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, string updateTime)
        {

            using (var metrics = new Metrics.Net.Metrics(queueName))
            {
                using (
                    var creator = SharedSetup.CreateCreator<TTransportInit>(InterceptorAdding.Yes, logProvider, metrics)
                    )
                {
                    using (
                        var queue =
                            creator.CreateConsumer(queueName, connectionString))
                    {
                        SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, workerCount, heartBeatTime,
                            heartBeatMonitorTime, updateTime);
                        queue.Configuration.TransportConfiguration.QueueDelayBehavior.Clear();
                        queue.Configuration.TransportConfiguration.QueueDelayBehavior.Add(TimeSpan.FromMilliseconds(100));
                        queue.Configuration.Worker.SingleWorkerWhenNoWorkFound = false;

                        waitForFinish.Reset();

                        //start looking for work
                        queue.Start<TTMessage>((message, notifications) =>
                        {
                            HandleFakeMessages(message, notifications, runTime, processedCount, messageCount,
                                waitForFinish);
                        });

                        waitForFinish.Wait(timeOut*1000);
                    }

                    Assert.Equal(messageCount, processedCount.ProcessedCount);
                    VerifyMetrics.VerifyProcessedCount(queueName, metrics.GetCurrentMetrics(), messageCount);
                }
            }
        }

        private void HandleFakeMessages(IReceivedMessage<TTMessage> message, IWorkerNotification notifications, int runTime,
            IncrementWrapper processedCount, int messageCount, ManualResetEventSlim waitForFinish)
        {
            if (runTime > 0)
                Thread.Sleep(runTime * 1000);

            var timeOut = message.GetHeader(notifications.HeaderNames.StandardHeaders.RpcTimeout).Timeout;

            var connection = message.GetHeader(notifications.HeaderNames.StandardHeaders.RpcConnectionInfo);
            if (connection == null)
            {
                throw new DotNetWorkQueueException("response connection was not set");
            }

            if (!_queues.ContainsKey(connection))
            {
                lock (_createQueue)
                {
                    if (!_queues.ContainsKey(connection))
                    {
                        var queue = CreateResponseQueue(connection);
                        if (!_queues.TryAdd(connection, queue))
                        {
                            queue.Dispose();
                        }

                    }
                }
            }

            var response = new TTResponse();
            _queues[connection].Send(response, _queues[connection].CreateResponse(message.MessageId, timeOut));

            Interlocked.Increment(ref processedCount.ProcessedCount);
            if (Interlocked.Read(ref processedCount.ProcessedCount) == messageCount)
            {
                waitForFinish.Set();
            }
        }
        private IProducerQueueRpc<TTResponse> CreateResponseQueue(IConnectionInformation connectionInfo)
        {
            //create the queue
            return
                _creator
                    .CreateProducerRpc<TTResponse>(connectionInfo.QueueName, connectionInfo.ConnectionString);
        }
    }
}
