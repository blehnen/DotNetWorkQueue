using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation
{
    public class ConsumerHeartbeat
    {
        public void Run<TTransportInit, TMessage, TTransportCreate>(
            QueueConnection queueConnection,
            int messageCount, int runtime, int timeOut, int workerCount, bool enableChaos,
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
                    Assert.True(result.Success, result.ErrorMessage);
                    scope = oCreation.Scope;

                    var producer = new ProducerShared();
                    producer.RunTest<TTransportInit, TMessage>(queueConnection, false, messageCount,
                        logProvider, generateData,
                        verify, false, oCreation.Scope, false);

                    var consumer = new ConsumerHeartBeatShared<TMessage>();
                    consumer.RunConsumer<TTransportInit>(queueConnection,
                        false,
                        logProvider,
                        runtime, messageCount,
                        workerCount, timeOut, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)",
                        null, enableChaos, scope);

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
