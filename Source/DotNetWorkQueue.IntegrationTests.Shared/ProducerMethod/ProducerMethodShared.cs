using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod
{
    public class ProducerMethodShared
    {
        public void RunTestCompiled<TTransportInit>(QueueConnection queueConnection,
            bool addInterceptors,
            long messageCount,
            ILogger logProvider,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            bool sendViaBatch, Guid id,
            Func<Guid, int, Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>
                generateTestMethod, int runTime, ICreationScope scope, bool enableChaos)
            where TTransportInit : ITransportInit, new()
        {
            RunTestCompiled<TTransportInit>(queueConnection, addInterceptors, messageCount, logProvider,
                generateData,
                verify, sendViaBatch, true, id, generateTestMethod, runTime, scope, enableChaos);
        }

#if NETFULL
        public void RunTestDynamic<TTransportInit>(QueueConnection queueConnection,
           bool addInterceptors,
           long messageCount,
           ILogger logProvider,
           Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
           Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
           bool sendViaBatch, Guid id,
           Func<Guid, int,LinqExpressionToRun>
               generateTestMethod, int runTime, ICreationScope scope, bool enableChaos)
           where TTransportInit : ITransportInit, new()
        {
            RunTestDynamic<TTransportInit>(queueConnection, addInterceptors, messageCount, logProvider,
                generateData,
                verify, sendViaBatch, true, id, generateTestMethod, runTime, scope, enableChaos);
        }
#endif

        public void RunTestCompiled<TTransportInit>(QueueConnection queueConnection,
            bool addInterceptors,
            long messageCount,
            ILogger logProvider,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            bool sendViaBatch, bool validateMetricCounts, Guid id,
            Func<Guid, int, Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>
                generateTestMethod, int runTime, ICreationScope scope, bool enableChaos)
            where TTransportInit : ITransportInit, new()
        {
            using (var metrics = new Metrics.Metrics(queueConnection.Queue))
            {
                var addInterceptorProducer = InterceptorAdding.No;
                if (addInterceptors)
                {
                    addInterceptorProducer = InterceptorAdding.Yes;
                }
                using (
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorProducer, logProvider, metrics, false, enableChaos, scope)
                    )
                {
                    //create the queue
                    using (var queue =
                        creator
                            .CreateMethodProducer(queueConnection))
                    {
                        RunProducerCompiled(queue, queueConnection, messageCount, generateData, verify, sendViaBatch, id,
                            generateTestMethod, runTime, scope);
                    }
                    if (validateMetricCounts)
                        VerifyMetrics.VerifyProducedCount(queueConnection.Queue, metrics.GetCurrentMetrics(), messageCount);
                }
            }
        }

#if NETFULL
        public void RunTestDynamic<TTransportInit>(QueueConnection queueConnection,
            bool addInterceptors,
            long messageCount,
            ILogger logProvider,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            bool sendViaBatch, bool validateMetricCounts, Guid id,
            Func<Guid, int, LinqExpressionToRun> generateTestMethod, int runTime, ICreationScope scope, bool enableChaos)
            where TTransportInit : ITransportInit, new()
        {
            using (var metrics = new Metrics.Metrics(queueConnection.Queue))
            {
                var addInterceptorProducer = InterceptorAdding.No;
                if (addInterceptors)
                {
                    addInterceptorProducer = InterceptorAdding.Yes;
                }
                using (
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorProducer, logProvider, metrics, false, enableChaos, scope)
                    )
                {
                    //create the queue
                    using (var queue =
                        creator
                            .CreateMethodProducer(queueConnection))
                    {
                        RunProducerDynamic(queue, queueConnection, messageCount, generateData, verify, sendViaBatch, id,
                            generateTestMethod, runTime, scope);
                    }
                    if (validateMetricCounts)
                        VerifyMetrics.VerifyProducedCount(queueConnection.Queue, metrics.GetCurrentMetrics(), messageCount);
                }
            }
        }
#endif

        private void RunProducerCompiled(
            IProducerMethodQueue queue,
            QueueConnection queueConnection,
            long messageCount,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            bool sendViaBatch, Guid id,
            Func<Guid, int, Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>
                generateTestMethod, int runTime, ICreationScope scope)
        {
            RunProducerCompiledInternal(queue, messageCount, generateData, sendViaBatch, id, generateTestMethod, runTime);
            LoggerShared.CheckForErrors(queueConnection.Queue);
            verify(queueConnection, queue.Configuration, messageCount, scope);
        }

#if NETFULL
        private void RunProducerDynamic(
            IProducerMethodQueue queue,
            QueueConnection queueConnection,
            long messageCount,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            bool sendViaBatch, Guid id,
            Func<Guid, int, LinqExpressionToRun> generateTestMethod, int runTime, ICreationScope scope)
        {
            RunProducerDynamicInternal(queue, messageCount, generateData, sendViaBatch, id, generateTestMethod, runTime);
            LoggerShared.CheckForErrors(queueConnection.Queue);
            verify(queueConnection, queue.Configuration, messageCount, scope);
        }
#endif

        private void RunProducerCompiledInternal(
            IProducerMethodQueue
                queue, long messageCount, Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            bool sendViaBatch, Guid id,
            Func<Guid, int, Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>
                generateTestMethod, int runTime)
        {
            var numberOfJobs = Convert.ToInt32(messageCount);
            var jobs = Enumerable.Range(0, numberOfJobs)
                .Select(i => generateTestMethod.Invoke(id, runTime));

            if (sendViaBatch)
            {
                var messages =
                    new List
                        <
                            QueueMessage
                                <Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>,
                                    IAdditionalMessageData>>(numberOfJobs);
                messages.AddRange(from job in jobs
                    let data = generateData(queue.Configuration)
                    select
                        data != null
                            ? new QueueMessage
                                <Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>,
                                    IAdditionalMessageData>(job, data)
                            : new QueueMessage
                                <Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>,
                                    IAdditionalMessageData>(job, null));
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

#if NETFULL
        private void RunProducerDynamicInternal(
            IProducerMethodQueue
                queue, long messageCount, Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            bool sendViaBatch, Guid id,
            Func<Guid, int, LinqExpressionToRun>
                generateTestMethod, int runTime)
        {
            var numberOfJobs = Convert.ToInt32(messageCount);
            var jobs = Enumerable.Range(0, numberOfJobs)
                .Select(i => generateTestMethod.Invoke(id, runTime));

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
#endif
    }
}
