using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation
{
    public class ConsumerMethodCancelWork
    {
        public void Run<TTransportInit, TTransportCreate>(
            QueueConnection queueConnection,
            int messageCount, int runtime,
            int timeOut, int workerCount, LinqMethodTypes linqMethodTypes, bool enableChaos,
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
                ICreationScope scope = null;
                var oCreation = queueCreator.GetQueueCreation<TTransportCreate>(queueConnection);
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
                            verify, false, id, GenerateMethod.CreateCancelCompiled, runtime,
                            scope, false);
                    }
                    else
                    {
                        producer.RunTestDynamic<TTransportInit>(queueConnection, false, messageCount,
                            logProvider, generateData,
                            verify, false, id, GenerateMethod.CreateCancelDynamic, runtime, scope,
                            false);

                    }

                    var consumer = new ConsumerMethodCancelWorkShared<TTransportInit>();
                    consumer.RunConsumer(queueConnection, false, logProvider,
                        runtime, messageCount,
                        workerCount, timeOut,
                        serviceRegister =>
                            serviceRegister.Register<IMessageMethodHandling>(
                                () => new MethodMessageProcessingCancel(id), LifeStyles.Singleton),
                        TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", id, enableChaos, scope);

                    verifyQueueCount(queueConnection, oCreation.BaseTransportOptions, scope, 0, false, false);
                    GenerateMethod.ClearCancel(id);

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
