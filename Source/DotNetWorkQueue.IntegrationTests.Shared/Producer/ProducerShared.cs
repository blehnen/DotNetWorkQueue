using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.Producer
{
    public class ProducerShared
    {
        public void RunTest<TTransportInit, TMessage>(QueueConnection queueConnection,
            bool addInterceptors,
            long messageCount,
            ILogger logProvider,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            bool sendViaBatch,
            ICreationScope scope, bool enableChaos)
            where TTransportInit : ITransportInit, new()
            where TMessage : class
        {
            RunTest<TTransportInit, TMessage>(queueConnection, addInterceptors, messageCount, logProvider, generateData,
                verify, sendViaBatch, true, scope, enableChaos);
        }

        public void RunTest<TTransportInit, TMessage>(QueueConnection queueConnection,
            bool addInterceptors,
            long messageCount,
            ILogger logProvider,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            bool sendViaBatch, bool validateMetricCounts,
            ICreationScope scope, bool enableChaos)
            where TTransportInit : ITransportInit, new()
            where TMessage : class
        {
            using (var metrics = new Metrics.Metrics(queueConnection.Queue))
            {
                var addInterceptorProducer = InterceptorAdding.No;
                if (addInterceptors)
                {
                    addInterceptorProducer = InterceptorAdding.Yes;
                }
                using (
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorProducer, logProvider, metrics, false, enableChaos)
                    )
                {
                    //create the queue
                    using (var queue =
                        creator
                            .CreateProducer
                            <TMessage>(queueConnection))
                    {
                        RunProducer(queue, queueConnection, messageCount, generateData, verify, sendViaBatch, scope);
                    }

                    if (validateMetricCounts)
                        VerifyMetrics.VerifyProducedCount(queueConnection.Queue, metrics.GetCurrentMetrics(), messageCount);
                }
            }
        }

        private void RunProducer<TMessage>(
          IProducerQueue
              <TMessage> queue, 
                QueueConnection queueConnection,
                long messageCount,
                Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
                Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
                bool sendViaBatch,
                ICreationScope scope)
            where TMessage: class
        {   
            RunProducerInternal(queue, messageCount, generateData, sendViaBatch);
            LoggerShared.CheckForErrors(queueConnection.Queue);
            verify(queueConnection, queue.Configuration, messageCount, scope);
        }

        private void RunProducerInternal<TMessage>(
           IProducerQueue
               <TMessage> queue, long messageCount, Func<QueueProducerConfiguration, AdditionalMessageData> generateData, bool sendViaBatch)
             where TMessage : class
        {
            var numberOfJobs = Convert.ToInt32(messageCount);
            var jobs = Enumerable.Range(0, numberOfJobs)
                .Select(i => GenerateMessage.Create<TMessage>());

            if (sendViaBatch)
            {
                var messages = new List<QueueMessage<TMessage, IAdditionalMessageData>>(numberOfJobs);
                messages.AddRange(from job in jobs let data = generateData(queue.Configuration) select data != null ? new QueueMessage<TMessage, IAdditionalMessageData>(job, data) : new QueueMessage<TMessage, IAdditionalMessageData>(job, null));
                var result = queue.Send(messages);
                var errorList = result.Where(p => result.Any(l => p.SendingException != null))
                           .ToList();
                if(result.HasErrors)
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
