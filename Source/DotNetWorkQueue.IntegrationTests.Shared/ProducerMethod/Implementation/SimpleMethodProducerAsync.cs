using System;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation
{
    public class SimpleMethodProducerAsync
    {
        public async Task Run<TTransportInit, TTransportCreate>(
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
            using (
                var queueCreator =
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

                    var producer = new ProducerMethodAsyncShared();
                    var id = Guid.NewGuid();
                    await producer.RunTestAsync<TTransportInit>(queueConnection, interceptors, messageCount,
                            logProvider,
                            generateData,
                            verify, sendViaBatch, 0, id, linqMethodTypes, oCreation.Scope, enableChaos)
                        .ConfigureAwait(false);
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
