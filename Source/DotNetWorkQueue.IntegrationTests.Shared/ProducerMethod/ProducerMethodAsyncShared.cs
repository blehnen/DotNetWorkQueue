using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod
{
    public class ProducerMethodAsyncShared
    {
        public async Task RunTestAsync<TTransportInit>(QueueConnection queueConnection,
            bool addInterceptors,
            long messageCount,
            ILogger logProvider,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify, bool sendViaBatch,
            int runTime, Guid id, LinqMethodTypes linqMethodTypes,
            ICreationScope scope, bool enableChaos)
            where TTransportInit : ITransportInit, new()
        {

            using (var trace = SharedSetup.CreateTrace("producer"))
            {
                using (var metrics = new Metrics.Metrics(queueConnection.Queue))
                {
                    var addInterceptorProducer = InterceptorAdding.No;
                    if (addInterceptors)
                    {
                        addInterceptorProducer = InterceptorAdding.Yes;
                    }

                    using (
                        var creator =
                        SharedSetup.CreateCreator<TTransportInit>(addInterceptorProducer, logProvider, metrics, false,
                            enableChaos, scope, trace.Source)
                    )
                    {
                        //create the queue
                        using (var queue =
                               creator
                                   .CreateMethodProducer(queueConnection))
                        {
                            await
                                RunProducerAsync(queue, queueConnection, messageCount, generateData, verify,
                                    sendViaBatch,
                                    runTime, id,
                                    linqMethodTypes, scope).ConfigureAwait(false);
                        }

                        VerifyMetrics.VerifyProducedAsyncCount(queueConnection.Queue, metrics.GetCurrentMetrics(),
                            messageCount);
                    }
                }
            }
        }

        private async Task RunProducerAsync(
            IProducerMethodQueue queue,
            QueueConnection queueConnection,
            long messageCount,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify, bool sendViaBatch,
            int runTime, Guid id, LinqMethodTypes type, ICreationScope scope)
        {
            await RunProducerInternalAsync(queue, messageCount, generateData, sendViaBatch, runTime, id, type)
                .ConfigureAwait(false);
            LoggerShared.CheckForErrors(queueConnection.Queue);
            verify(queueConnection,
                queue.Configuration, messageCount, scope);
        }

        private async Task RunProducerInternalAsync(
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
                    await RunProducerInternalAsync(queue, generateData, sendViaBatch, jobs, numberOfJobs)
                        .ConfigureAwait(false);
                }
                    break;

#if NETFULL
                case LinqMethodTypes.Dynamic:
                {
                    var jobs = Enumerable.Range(0, numberOfJobs)
                        .Select(i => GenerateMethod.CreateDynamic(id, runTime));
                    await RunProducerInternalAsync(queue, generateData, sendViaBatch, jobs, numberOfJobs)
                        .ConfigureAwait(false);
                }
                    break;
#endif
            }
        }

        private async Task RunProducerInternalAsync(
            IProducerMethodQueue
                queue, Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            bool sendViaBatch,
            IEnumerable<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>> jobs,
            int numberOfJobs)
        {
            if (sendViaBatch)
            {
                var messages =
                    new List<QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>,
                        IAdditionalMessageData>>(numberOfJobs);
                messages.AddRange(from job in jobs
                    let data = generateData(queue.Configuration)
                    select data != null
                        ? new QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>,
                            IAdditionalMessageData>(job, data)
                        : new QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>,
                            IAdditionalMessageData>(job, null));
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

#if NETFULL
        private async Task RunProducerInternalAsync(
          IProducerMethodQueue
              queue, Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
                   bool sendViaBatch, IEnumerable<LinqExpressionToRun> jobs, int numberOfJobs)
        {
            if (sendViaBatch)
            {
                var messages = new List<QueueMessage<LinqExpressionToRun, IAdditionalMessageData>>(numberOfJobs);
                messages.AddRange(from job in jobs let data =
generateData(queue.Configuration) select data != null ? new QueueMessage<LinqExpressionToRun, IAdditionalMessageData>(job, data) : new QueueMessage<LinqExpressionToRun, IAdditionalMessageData>(job, null));
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
#endif
    }
}
