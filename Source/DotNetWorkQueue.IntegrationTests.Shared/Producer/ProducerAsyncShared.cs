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
    public class ProducerAsyncShared
    {
        public async Task RunTestAsync<TTransportInit, TMessage>(QueueConnection queueConnection,
            bool addInterceptors,
            long messageCount,
            ILogger logProvider,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify, bool sendViaBatch,
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
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorProducer, logProvider, metrics, false, enableChaos, scope)
                    )
                {
                    //create the queue
                    using (var queue =
                        creator
                            .CreateProducer
                            <TMessage>(queueConnection))
                    {
                        await RunProducerAsync(queue, queueConnection, messageCount, generateData, verify, sendViaBatch, scope).ConfigureAwait(false);
                    }
                    VerifyMetrics.VerifyProducedAsyncCount(queueConnection.Queue, metrics.GetCurrentMetrics(), messageCount);
                }
            }
        }

        private async Task RunProducerAsync<TMessage>(
          IProducerQueue
              <TMessage> queue,
               QueueConnection queueConnection,
                long messageCount,
                Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
                Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify, bool sendViaBatch,
                ICreationScope scope)
            where TMessage : class
        {
            await RunProducerInternalAsync(queue, messageCount, generateData, sendViaBatch).ConfigureAwait(false);
            LoggerShared.CheckForErrors(queueConnection.Queue);
            verify(queueConnection, queue.Configuration, messageCount, scope);
        }

        private async Task RunProducerInternalAsync<TMessage>(
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
