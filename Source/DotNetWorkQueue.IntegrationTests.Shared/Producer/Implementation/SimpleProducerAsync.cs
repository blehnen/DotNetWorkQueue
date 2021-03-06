﻿using System;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using Xunit;
namespace DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation
{
    public class SimpleProducerAsync
    {
        public async Task<bool> Run<TTransportInit, TMessage, TTransportCreate>(
            QueueConnection queueConnection,
            int messageCount,
            bool interceptors,
            bool enableChaos,
            bool sendViaBatch,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify)
            where TTransportInit : ITransportInit, new()
            where TMessage : class
            where TTransportCreate : class, IQueueCreation
        {

            var logProvider = LoggerShared.Create(queueConnection.Queue, GetType().Name);
            using (
                var queueCreator =
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

                    var producer = new ProducerAsyncShared();
                    await producer.RunTestAsync<TTransportInit, TMessage>(queueConnection,
                        interceptors, messageCount, logProvider,
                        generateData,
                        verify, sendViaBatch, oCreation.Scope, enableChaos).ConfigureAwait(false);

                }
                finally
                {
                    oCreation?.RemoveQueue();
                    oCreation?.Dispose();
                    scope?.Dispose();
                }

                return true;
            }
        }
    }
}
