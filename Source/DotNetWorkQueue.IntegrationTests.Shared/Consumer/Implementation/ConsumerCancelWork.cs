using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation
{
    public class ConsumerCancelWork
    {
        public void Run<TTransportInit, TMessage, TTransportCreate>(
            string queueName,
            string connectionString,
            int messageCount,
            int runtime,
            int timeOut,
            int workerCount,
            bool enableChaos,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            Action<string, string, IBaseTransportOptions, ICreationScope, int, bool, bool> verifyQueueCount)
            where TTransportInit : ITransportInit, new()
            where TMessage : class
            where TTransportCreate : class, IQueueCreation
        {

            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (
                var queueCreator =
                    new QueueCreationContainer<TTransportInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var queueConnection = new QueueConnection(queueName, connectionString);
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

                    var consumer = new ConsumerCancelWorkShared<TTransportInit, TMessage>();
                    consumer.RunConsumer(queueConnection, false, logProvider,
                        runtime, messageCount,
                        workerCount, timeOut, x => x.RegisterNonScopedSingleton(scope), TimeSpan.FromSeconds(30),
                        TimeSpan.FromSeconds(35), "second(*%10)", null, enableChaos, scope);

                    verifyQueueCount(queueName, connectionString, oCreation.BaseTransportOptions, scope,
                        0, false, false);

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
