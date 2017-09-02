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
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Metrics.Net;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod
{
    public class ProducerMethodMultipleDynamicShared
    {
        public void RunTestDynamic<TTransportInit>(string queueName,
           string connectionString,
           bool addInterceptors,
           long messageCount,
           ILogProvider logProvider,
           Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
           Action<string, string, QueueProducerConfiguration, long, ICreationScope> verify,
           bool sendViaBatch, Guid id,
           Func<Guid, int, int, LinqExpressionToRun> generateTestMethod, 
           int runTime, ICreationScope scope)
           where TTransportInit : ITransportInit, new()
        {
            RunTestDynamic<TTransportInit>(queueName, connectionString, addInterceptors, messageCount, logProvider,
                generateData,
                verify, sendViaBatch, true, id, generateTestMethod, runTime, scope);
        }

        public void RunTestDynamic<TTransportInit>(string queueName,
            string connectionString,
            bool addInterceptors,
            long messageCount,
            ILogProvider logProvider,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<string, string, QueueProducerConfiguration, long, ICreationScope> verify,
            bool sendViaBatch, bool validateMetricCounts, Guid id,
            Func<Guid, int, int, LinqExpressionToRun> generateTestMethod, 
            int runTime, ICreationScope scope)
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
                        RunProducerDynamic(queue, queueName, messageCount, generateData, verify, sendViaBatch, id,
                            generateTestMethod, runTime, scope);
                    }
                    if (validateMetricCounts)
                        VerifyMetrics.VerifyProducedCount(queueName, metrics.GetCurrentMetrics(), messageCount);
                }
            }
        }

        private void RunProducerDynamic(
            IProducerMethodQueue
                queue,
            string queueName,
            long messageCount,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<string, string, QueueProducerConfiguration, long, ICreationScope> verify,
            bool sendViaBatch, Guid id,
            Func<Guid, int, int, LinqExpressionToRun> generateTestMethod, int runTime, ICreationScope scope)
        {
            RunProducerDynamicInternal(queue, messageCount, generateData, sendViaBatch, id, generateTestMethod, runTime);
            LoggerShared.CheckForErrors(queueName);
            verify(queueName, queue.Configuration.TransportConfiguration.ConnectionInfo.ConnectionString,
                queue.Configuration, messageCount, scope);
        }

        private void RunProducerDynamicInternal(
            IProducerMethodQueue
                queue, long messageCount,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            bool sendViaBatch, Guid id,
            Func<Guid, int, int, LinqExpressionToRun> generateTestMethod, 
            int runTime)
        {
            var numberOfJobs = Convert.ToInt32(messageCount);
            var jobs = Enumerable.Range(0, numberOfJobs)
                .Select(i => generateTestMethod.Invoke(id, i, runTime));

            if (sendViaBatch)
            {
                var messages = new List<QueueMessage<LinqExpressionToRun, IAdditionalMessageData>>(numberOfJobs);
                messages.AddRange(from job in jobs
                                  let data = generateData(queue.Configuration)
                                  select
                                      data != null
                                          ? new QueueMessage<LinqExpressionToRun, IAdditionalMessageData>(job, data)
                                          : new QueueMessage<LinqExpressionToRun, IAdditionalMessageData>(job, null));
                var result = queue.Send(messages);
                var errorList = result.Where(p => result.Any(l => p.SendingException != null))
                    .ToList();
                if (result.HasErrors)
                {
                    Assert.False(result.HasErrors, errorList[0].SendingException.ToString());
                }
                else
                {
                    Assert.False(result.HasErrors);
                }
            }
            else
            {
                Parallel.ForEach(jobs, job =>
                {
                    var data = generateData(queue.Configuration);
                    if (data != null)
                    {
                        var result = queue.Send(job, data);
                        var message = string.Empty;
                        if (result.SendingException != null)
                        {
                            message = result.SendingException.ToString();
                        }
                        Assert.False(result.HasError, message);
                    }
                    else
                    {
                        var result = queue.Send(job);
                        var message = string.Empty;
                        if (result.SendingException != null)
                        {
                            message = result.SendingException.ToString();
                        }
                        Assert.False(result.HasError, message);
                    }
                });
            }
        }
    }
}
