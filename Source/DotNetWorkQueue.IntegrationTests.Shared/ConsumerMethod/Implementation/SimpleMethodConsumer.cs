using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation
{
    public class SimpleMethodConsumer
    {
        public void Run<TTransportInit, TTransportCreate>(
            QueueConnection queueConnection,
            int messageCount, int runtime, int timeOut, int workerCount, LinqMethodTypes linqMethodTypes,
            bool enableChaos,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            Action<QueueConnection, IBaseTransportOptions, ICreationScope, int, bool, bool> verifyQueueCount)
            where TTransportInit : ITransportInit, new()
            where TTransportCreate : class, IQueueCreation
        {

            var logProvider = LoggerShared.Create(queueConnection.Queue, GetType().Name);
            using (
                var queueCreator =
                    new QueueCreationContainer<TTransportInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var oCreation =
                    queueCreator.GetQueueCreation<TTransportCreate>(queueConnection);
                ICreationScope scope = null;
                try
                {
                    setOptions(oCreation);
                    var result = oCreation.CreateQueue();
                    Assert.True(result.Success, result.ErrorMessage);
                    scope = oCreation.Scope;

                    var producer = new ProducerMethodShared();
                    var id = Guid.NewGuid();
                    if (linqMethodTypes == LinqMethodTypes.Compiled)
                    {
                        producer.RunTestCompiled<TTransportInit>(queueConnection, false, messageCount,
                            logProvider, generateData,
                            verify, false, id, GenerateMethod.CreateCompiled, runtime, oCreation.Scope,
                            false);
                    }
                    else
                    {
                        producer.RunTestDynamic<TTransportInit>(queueConnection, false, messageCount,
                            logProvider, generateData,
                            verify, false, id, GenerateMethod.CreateDynamic, runtime, oCreation.Scope,
                            false);
                    }

                    var consumer = new ConsumerMethodShared();
                    consumer.RunConsumer<TTransportInit>(queueConnection,
                        false,
                        logProvider,
                        runtime, messageCount,
                        workerCount, timeOut,
                        TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id, "second(*%10)", enableChaos,
                        scope);

                    verifyQueueCount(queueConnection, oCreation.BaseTransportOptions, scope, 0, false, false);
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
