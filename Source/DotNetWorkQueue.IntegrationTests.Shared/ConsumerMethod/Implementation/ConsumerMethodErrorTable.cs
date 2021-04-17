using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation
{
    public class ConsumerMethodErrorTable
    {
        public void Run<TTransportInit, TTransportCreate>(
            QueueConnection queueConnection,
            int messageCount, int timeOut, int workerCount, LinqMethodTypes linqMethodTypes,
            bool enableChaos,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            Action<QueueConnection, IBaseTransportOptions, ICreationScope, int, bool, bool> verifyQueueCount,
            Action<QueueConnection, int, ICreationScope> validateErrorCounts)
            where TTransportInit : ITransportInit, new()
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

                        //create data
                        var producer = new ProducerMethodShared();
                        var id = Guid.NewGuid();
                        if (linqMethodTypes == LinqMethodTypes.Compiled)
                        {
                            producer.RunTestCompiled<TTransportInit>(queueConnection, false, messageCount,
                                logProvider, generateData,
                                verify, false, id, GenerateMethod.CreateErrorCompiled, 0, oCreation.Scope,
                                false);
                        }
                        else
                        {
                            producer.RunTestDynamic<TTransportInit>(queueConnection, false, messageCount,
                                logProvider, generateData,
                                verify, false, id, GenerateMethod.CreateErrorDynamic, 0, oCreation.Scope,
                                false);

                        }

                        //process data
                        var consumer = new ConsumerMethodErrorShared();
                        consumer.RunConsumer<TTransportInit>(queueConnection,
                            false,
                            logProvider,
                            workerCount, timeOut, messageCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id,
                            "second(*%10)", enableChaos, scope);
                        validateErrorCounts(queueConnection, messageCount, scope);
                        verifyQueueCount(queueConnection, oCreation.BaseTransportOptions, scope, messageCount, true, false);

                        consumer.PurgeErrorMessages<TTransportInit>(queueConnection,
                            false, logProvider, false, scope);
                        validateErrorCounts(queueConnection, messageCount, scope);

                        //purge error messages and verify that count is 0
                        consumer.PurgeErrorMessages<TTransportInit>(queueConnection,
                            false, logProvider, true, scope);
                        validateErrorCounts(queueConnection, 0, scope);

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
