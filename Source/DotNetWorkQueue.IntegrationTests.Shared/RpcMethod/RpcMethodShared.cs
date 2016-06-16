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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Metrics.Net;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.RpcMethod
{
    public class RpcMethodShared<TTransportInit, TTResponse, TTConnectionSettings>
        where TTransportInit : ITransportInit, new()
        where TTResponse: class, new()
        where TTConnectionSettings : BaseRpcConnection
    {
        public void Run(string queueNameReceive, string queueNameSend, string connectionStringReceive,
            string connectionStringSend, ILogProvider logProviderReceive, ILogProvider logProviderSend,
            int runtime, int messageCount, int workerCount, int timeOut, bool async, TTConnectionSettings rpcConnection,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, Guid id, LinqMethodTypes linqMethodTypes)
        {
            var task1 = Task.Factory.StartNew(() =>
                RunRpcReceive(queueNameSend, connectionStringSend, logProviderSend,
                    runtime, messageCount,
                    workerCount, timeOut, heartBeatTime, heartBeatMonitorTime, id));

            RunRpcSend(logProviderSend, messageCount, async, rpcConnection, runtime, id, linqMethodTypes);

            Task.WaitAll(task1);
            LoggerShared.CheckForErrors(queueNameSend);
            LoggerShared.CheckForErrors(queueNameReceive);
        }

        private void RunRpcSend(ILogProvider logProvider, int messageCount, bool async, TTConnectionSettings rpcConnection, int runTime, Guid id, LinqMethodTypes linqMethodTypes)
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
                            .CreateMethodRpc(rpcConnection))
                {

                    queue.Configuration.TransportConfigurationReceive.QueueDelayBehavior.Clear();
                    queue.Configuration.TransportConfigurationReceive.QueueDelayBehavior.Add(
                        TimeSpan.FromMilliseconds(500));
                    queue.Configuration.MessageExpiration.MonitorTime = TimeSpan.FromSeconds(10);
                    queue.Start();

                    if (async)
                    {
                        SendMultipleMessagesAsync(queue, messageCount, id, runTime, linqMethodTypes);
                    }
                    else
                    {
                        SendMultipleMessages(queue, messageCount, id, runTime, linqMethodTypes);
                    }
                }
            }
        }

        private static void SendMultipleMessages(IRpcMethodQueue queue, int number, Guid id, int runTime, LinqMethodTypes linqMethodTypes)
        {
            switch (linqMethodTypes)
            {
                case LinqMethodTypes.Compiled:
                {
                    var numberOfJobs = number;
                    var jobs = Enumerable.Range(0, numberOfJobs)
                        .Select(i => GenerateMethod.CreateRpcCompiled(id, runTime));
                    SendMultipleMessages(queue, jobs);
                }
                break;
                case LinqMethodTypes.Dynamic:
                {
                        var numberOfJobs = number;
                        var jobs = Enumerable.Range(0, numberOfJobs)
                            .Select(i => GenerateMethod.CreateRpcDynamic(id, runTime));
                        SendMultipleMessages(queue, jobs);
                    }
                break;
            }
        }

        private static void SendMultipleMessages(IRpcMethodQueue queue,
            IEnumerable<LinqExpressionToRun> jobs)
        {
            Parallel.ForEach(jobs, job =>
            {
                try
                {
                    var message = queue.Send(job, TimeSpan.FromSeconds(30));
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

        private static void SendMultipleMessages(IRpcMethodQueue queue,
           IEnumerable<Expression<Func<IReceivedMessage<MessageExpression>, IWorkerNotification, object>>> jobs)
        {
            Parallel.ForEach(jobs, job =>
            {
                try
                {
                    var message = queue.Send(job, TimeSpan.FromSeconds(30));
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

        private static async void SendMultipleMessagesAsync(IRpcMethodQueue queue, int number, Guid id, int runTime, LinqMethodTypes linqMethodTypes)
        {
            switch (linqMethodTypes)
            {
                case LinqMethodTypes.Compiled:
                    {
                        var numberOfJobs = number;
                        var jobs = Enumerable.Range(0, numberOfJobs)
                            .Select(i => GenerateMethod.CreateRpcCompiled(id, runTime));
                        await SendMultipleMessagesAsync(queue, jobs).ConfigureAwait(false);
                    }
                    break;
                case LinqMethodTypes.Dynamic:
                    {
                        var numberOfJobs = number;
                        var jobs = Enumerable.Range(0, numberOfJobs)
                            .Select(i => GenerateMethod.CreateRpcDynamic(id, runTime));
                        await SendMultipleMessagesAsync(queue, jobs).ConfigureAwait(false);
                    }
                    break;
            }
        }

        private static async Task SendMultipleMessagesAsync(IRpcMethodQueue queue, IEnumerable<Expression<Func<IReceivedMessage<MessageExpression>, IWorkerNotification, object>>> jobs)
        {
            Parallel.ForEach(jobs, async job =>
            {
                try
                {
                    var message = await queue.SendAsync(job, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
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

        private static async Task SendMultipleMessagesAsync(IRpcMethodQueue queue, IEnumerable<LinqExpressionToRun> jobs)
        {
            Parallel.ForEach(jobs, async job =>
            {
                try
                {
                    var message = await queue.SendAsync(job, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
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
            int runTime, int messageCount,
            int workerCount, int timeOut,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, Guid id)
        {

            using (var metrics = new Metrics.Net.Metrics(queueName))
            {
                var creator = SharedSetup.CreateCreator<TTransportInit>(InterceptorAdding.Yes, logProvider, metrics);
                using (
                    var queue =
                        creator.CreateMethodConsumer(queueName, connectionString))
                {
                    SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, workerCount, heartBeatTime,
                        heartBeatMonitorTime);
                    queue.Configuration.TransportConfiguration.QueueDelayBehavior.Clear();
                    queue.Configuration.TransportConfiguration.QueueDelayBehavior.Add(TimeSpan.FromMilliseconds(100));
                    queue.Configuration.Worker.SingleWorkerWhenNoWorkFound = false;
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
            }
        }
    }
}
