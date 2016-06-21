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
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Metrics.Net;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod
{
    public class ProducerMethodAsyncShared
    {
        public async Task RunTest<TTransportInit>(string queueName,
            string connectionString,
            bool addInterceptors,
            long messageCount,
            ILogProvider logProvider,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<string, string, QueueProducerConfiguration, long> verify, bool sendViaBatch, int runTime, Guid id, LinqMethodTypes linqMethodTypes)
            where TTransportInit : ITransportInit, new()
        {

            using (var metrics = new Metrics.Net.Metrics(queueName))
            {
                var addInterceptorProducer = InterceptorAdding.No;
                if (addInterceptors)
                {
                    addInterceptorProducer = InterceptorAdding.Yes;
                }
                using (
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorProducer, logProvider, metrics)
                    )
                {
                    //create the queue
                    using (var queue =
                        creator
                            .CreateMethodProducer(queueName, connectionString))
                    {
                        await
                            RunProducer(queue, queueName, messageCount, generateData, verify, sendViaBatch, runTime, id,
                                linqMethodTypes);
                    }
                    VerifyMetrics.VerifyProducedAsyncCount(queueName, metrics.GetCurrentMetrics(), messageCount);
                }
            }
        }

        private async Task RunProducer(
          IProducerMethodQueue
              queue,
                string queueName,
                long messageCount,
                Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
                Action<string, string, QueueProducerConfiguration, long> verify, bool sendViaBatch, int runTime, Guid id, LinqMethodTypes type)
        {
            await RunProducerInternal(queue, messageCount, generateData, sendViaBatch, runTime, id, type).ConfigureAwait(false);
            LoggerShared.CheckForErrors(queueName);
            verify(queueName, queue.Configuration.TransportConfiguration.ConnectionInfo.ConnectionString, queue.Configuration, messageCount);
        }

        private async Task RunProducerInternal(
           IProducerMethodQueue
               queue, long messageCount, Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
                    bool sendViaBatch, int runTime, Guid id, LinqMethodTypes methodType)
        {
            var numberOfJobs = Convert.ToInt32(messageCount);
            switch (methodType)
            {
                case LinqMethodTypes.Compiled:
                {
                    var jobs = Enumerable.Range(0, numberOfJobs)
                        .Select(i => GenerateMethod.CreateCompiled(id, runTime));
                    await RunProducerInternal(queue, generateData, sendViaBatch, jobs, numberOfJobs).ConfigureAwait(false);
                }
                    break;
                case LinqMethodTypes.Dynamic:
                {
                    var jobs = Enumerable.Range(0, numberOfJobs)
                        .Select(i => GenerateMethod.CreateDynamic(id, runTime));
                    await RunProducerInternal(queue, generateData, sendViaBatch, jobs, numberOfJobs).ConfigureAwait(false);
                }
                    break;
            }
        }

        private async Task RunProducerInternal(
           IProducerMethodQueue
               queue, Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
                    bool sendViaBatch, IEnumerable<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>> jobs, int numberOfJobs)
        {
            if (sendViaBatch)
            {
                var messages = new List<QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>, IAdditionalMessageData>>(numberOfJobs);
                messages.AddRange(from job in jobs let data = generateData(queue.Configuration) select data != null ? new QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>, IAdditionalMessageData>(job, data) : new QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>, IAdditionalMessageData>(job, null));
                var results = await queue.SendAsync(messages).ConfigureAwait(false);
                Assert.False(results.HasErrors);
            }
            else
            {
                foreach (var job in jobs)
                {
                    var data = generateData(queue.Configuration);
                    if (data != null)
                    {
                        var result = await queue.SendAsync(job, data).ConfigureAwait(false);
                        Assert.False(result.HasError);
                    }
                    else
                    {
                        var result = await queue.SendAsync(job).ConfigureAwait(false);
                        Assert.False(result.HasError);
                    }
                }
            }
        }

        private async Task RunProducerInternal(
          IProducerMethodQueue
              queue, Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
                   bool sendViaBatch, IEnumerable<LinqExpressionToRun> jobs, int numberOfJobs)
        {
            if (sendViaBatch)
            {
                var messages = new List<QueueMessage<LinqExpressionToRun, IAdditionalMessageData>>(numberOfJobs);
                messages.AddRange(from job in jobs let data = generateData(queue.Configuration) select data != null ? new QueueMessage<LinqExpressionToRun, IAdditionalMessageData>(job, data) : new QueueMessage<LinqExpressionToRun, IAdditionalMessageData>(job, null));
                var results = await queue.SendAsync(messages).ConfigureAwait(false);
                Assert.False(results.HasErrors);
            }
            else
            {
                foreach (var job in jobs)
                {
                    var data = generateData(queue.Configuration);
                    if (data != null)
                    {
                        var result = await queue.SendAsync(job, data).ConfigureAwait(false);
                        Assert.False(result.HasError);
                    }
                    else
                    {
                        var result = await queue.SendAsync(job).ConfigureAwait(false);
                        Assert.False(result.HasError);
                    }
                }
            }
        }
    }
}
