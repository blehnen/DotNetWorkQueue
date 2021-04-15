using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync.Implementation
{
    public class ConsumerMethodAsyncErrorTable
    {
        public void Run<TTransportInit, TTransportCreate>(
            string queueName,
            string connectionString,
            int messageCount, int timeOut, int workerCount, int readerCount, int queueSize, LinqMethodTypes linqMethodTypes,
            bool enableChaos,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            Action<string, string, IBaseTransportOptions, ICreationScope, int, bool, bool> verifyQueueCount,
            Action<string, string, int, ICreationScope> validateErrorCounts)
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

                    //create data
                    var producer = new ProducerMethodShared();
                    var id = Guid.NewGuid();
                    if (linqMethodTypes == LinqMethodTypes.Compiled)
                    {
                        producer.RunTestCompiled<TTransportInit>(queueConnection, false, messageCount,
                            logProvider,
                            generateData,
                            verify, false, id, GenerateMethod.CreateErrorCompiled, 0, oCreation.Scope,
                            false);
                    }
                    else
                    {
                        producer.RunTestDynamic<TTransportInit>(queueConnection, false, messageCount,
                            logProvider,
                            generateData,
                            verify, false, id, GenerateMethod.CreateErrorDynamic, 0, oCreation.Scope,
                            false);
                    }

                    //process data
                    var consumer = new ConsumerMethodAsyncErrorShared();
                    consumer.RunConsumer<TTransportInit>(queueConnection,
                        false,
                        logProvider,
                        messageCount, workerCount, timeOut, queueSize, readerCount, TimeSpan.FromSeconds(30),
                        TimeSpan.FromSeconds(35), id, "second(*%10)", enableChaos, scope);
                    validateErrorCounts(queueName, connectionString, messageCount, scope);
                    verifyQueueCount(queueName, connectionString, oCreation.BaseTransportOptions, scope, messageCount, true, false);

                    consumer.PurgeErrorMessages<TTransportInit>(queueConnection,
                        false, logProvider, false, scope);
                    validateErrorCounts(queueName, connectionString, messageCount, scope);

                    //purge error messages and verify that count is 0
                    consumer.PurgeErrorMessages<TTransportInit>(queueConnection,
                        false, logProvider, true, scope);
                    validateErrorCounts(queueName, connectionString, 0, scope);

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
