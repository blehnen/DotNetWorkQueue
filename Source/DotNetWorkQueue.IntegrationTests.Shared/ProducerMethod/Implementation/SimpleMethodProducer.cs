using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation
{
    [Collection("Producer")]
    public class SimpleMethodProducer
    {
        public void Run<TTransportInit, TTransportCreate>(
            string queueName,
            string connectionString,
            int messageCount,
            LinqMethodTypes linqMethodTypes,
            bool interceptors,
            bool enableChaos,
            bool sendViaBatch,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify)
            where TTransportInit : ITransportInit, new()
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

                    var id = Guid.NewGuid();
                    var producer = new ProducerMethodShared();
                    if (linqMethodTypes == LinqMethodTypes.Compiled)
                    {
                        producer.RunTestCompiled<TTransportInit>(queueConnection, interceptors,
                            messageCount, logProvider,
                            generateData,
                            verify, sendViaBatch, id, GenerateMethod.CreateCompiled, 0, oCreation.Scope,
                            enableChaos);
                    }
                    else
                    {
                        producer.RunTestDynamic<TTransportInit>(queueConnection, interceptors, messageCount,
                            logProvider,
                            generateData,
                            verify, sendViaBatch, id, GenerateMethod.CreateDynamic, 0, oCreation.Scope,
                            enableChaos);
                    }

                }
                finally
                {
                    oCreation.RemoveQueue();
                    oCreation.Dispose();
                    scope?.Dispose();
                }
            }
        }
    }
}
