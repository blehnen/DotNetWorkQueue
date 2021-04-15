﻿using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation
{
    public class ConsumerMethodRollBack
    {
        public void Run<TTransportInit, TTransportCreate>(
            string queueName,
            string connectionString,
            int messageCount, int runtime, int timeOut, int workerCount, LinqMethodTypes linqMethodTypes,
            bool enableChaos,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            Action<string, string, IBaseTransportOptions, ICreationScope, int, bool, bool> verifyQueueCount)
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
                            logProvider, generateData,
                            verify, false, id, GenerateMethod.CreateRollBackCompiled, runtime,
                            oCreation.Scope, false);
                    }
                    else
                    {
                        producer.RunTestDynamic<TTransportInit>(queueConnection, false, messageCount,
                            logProvider, generateData,
                            verify, false, id, GenerateMethod.CreateRollBackDynamic, runtime,
                            oCreation.Scope, false);
                    }

                    //process data
                    var consumer = new ConsumerMethodRollBackShared();
                    consumer.RunConsumer<TTransportInit>(queueConnection,
                        false,
                        workerCount, logProvider, timeOut, runtime, messageCount,
                        TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", id, enableChaos, scope);

                    verifyQueueCount(queueName, connectionString, oCreation.BaseTransportOptions, scope, 0, false, false);
                    GenerateMethod.ClearRollback(id);

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