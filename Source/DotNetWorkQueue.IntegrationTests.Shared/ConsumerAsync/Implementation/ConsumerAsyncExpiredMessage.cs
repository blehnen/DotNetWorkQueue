using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.IoC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation
{
    /// <summary>
    /// Expired messages on the asynchronous consumer. Mirrors
    /// <see cref="Consumer.Implementation.ConsumerExpiredMessage"/>, which only ever covered the
    /// synchronous one.
    /// </summary>
    public class ConsumerAsyncExpiredMessage
    {
        public void Run<TTransportInit, TMessage, TTransportCreate>(
            QueueConnection queueConnection,
            int messageCount, int timeOut, int workerCount, int readerCount, bool enableChaos,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            Action<QueueConnection, IBaseTransportOptions, ICreationScope, int, bool, bool> verifyQueueCount)
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
                    Assert.IsTrue(result.Success, result.ErrorMessage);
                    scope = oCreation.Scope;

                    var producer = new ProducerShared();
                    producer.RunTest<TTransportInit, TMessage>(queueConnection, false, messageCount,
                        logProvider, generateData, verify, false, oCreation.Scope, false);

                    var consumer = new ConsumerAsyncExpiredMessageShared<TMessage>();
                    consumer.RunConsumer<TTransportInit>(queueConnection,
                        false,
                        workerCount, logProvider,
                        timeOut, readerCount, messageCount, TimeSpan.FromSeconds(30),
                        TimeSpan.FromSeconds(35), "*/10 * * * * *", null, enableChaos, scope);

                    verifyQueueCount(queueConnection, oCreation.BaseTransportOptions, scope, 0, false, false);
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
