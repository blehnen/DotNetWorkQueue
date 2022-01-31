using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation
{
    public class MultiProducer
    {
        public void Run<TTransportInit, TMessage, TTransportCreate>(
            QueueConnection queueConnection,
            int messageCount,
            bool enableChaos,
            int queueCount,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            Action<QueueConnection, IBaseTransportOptions, ICreationScope, long, long, string> verifyQueueData)
            where TTransportInit : ITransportInit, new()
            where TMessage : class
            where TTransportCreate : class, IQueueCreation
        {

            var logProvider = LoggerShared.Create(queueConnection.Queue, GetType().Name);
            using (var queueCreator =
                new QueueCreationContainer<TTransportInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                ICreationScope scope = null;
                var oCreation = queueCreator.GetQueueCreation<TTransportCreate>(queueConnection);
                try
                {
                    setOptions(oCreation);
                    var result = oCreation.CreateQueue();
                    Assert.True(result.Success, result.ErrorMessage);
                    scope = oCreation.Scope;

                    RunTest<TTransportInit, TMessage>(queueConnection, messageCount, queueCount, logProvider, oCreation.Scope, enableChaos,
                         generateData, verify);
                    LoggerShared.CheckForErrors(queueConnection.Queue);
                    verifyQueueData(queueConnection, oCreation.BaseTransportOptions, oCreation.Scope, messageCount, queueCount, null);
                }
                finally
                {
                    oCreation?.RemoveQueue();
                    oCreation?.Dispose();
                    scope?.Dispose();
                }
            }
        }

        private void RunTest<TTransportInit, TMessage>(QueueConnection queueConnection, int messageCount,
            int queueCount, ILogger logProvider, ICreationScope scope,
            bool enableChaos,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify)
            where TTransportInit : ITransportInit, new()
            where TMessage : class
        {
            var tasks = new List<Task>(queueCount);
            for (var i = 0; i < queueCount; i++)
            {
                var producer = new ProducerShared();
                var task = new Task(() => producer.RunTest<TTransportInit, TMessage>(queueConnection, false, messageCount,
                    logProvider, generateData, Verify, true, false, scope, enableChaos));
                tasks.Add(task);
            }
            tasks.AsParallel().ForAll(x => x.Start());
            Task.WaitAll(tasks.ToArray());
        }

        private void Verify(QueueConnection arg1, QueueProducerConfiguration arg2, long arg3, ICreationScope arg4)
        {
            //don't verify the single producers; verify at the end
        }
    }
}
