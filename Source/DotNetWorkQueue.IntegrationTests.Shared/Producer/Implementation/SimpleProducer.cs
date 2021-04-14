using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation
{
    public class SimpleProducer
    {
        public void Run<TTransportInit, TMessage, TTransportCreate>(
            string queueName,
            string connectionString,
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
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (var queueCreator =
                new QueueCreationContainer<TTransportInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var queueConnection =
                    new DotNetWorkQueue.Configuration.QueueConnection(queueName, connectionString);
                ICreationScope scope = null;
                var oCreation = queueCreator.GetQueueCreation<TTransportCreate>(queueConnection);
                try
                {
                    setOptions(oCreation);
                    var result = oCreation.CreateQueue();
                    Assert.True(result.Success, result.ErrorMessage);
                    scope = oCreation.Scope;

                    var producer = new ProducerShared();
                    producer.RunTest<TTransportInit, TMessage>(queueConnection, interceptors,
                        messageCount, logProvider,
                        generateData,
                        verify, sendViaBatch, oCreation.Scope, enableChaos);
                }
                finally
                {
                    oCreation?.RemoveQueue();
                    oCreation?.Dispose();
                    scope?.Dispose();
                }
            }
        }
    }
}
