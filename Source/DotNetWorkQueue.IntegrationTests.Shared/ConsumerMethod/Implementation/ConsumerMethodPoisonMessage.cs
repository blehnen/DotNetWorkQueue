﻿using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation
{
    public class ConsumerMethodPoisonMessage
    {
        public void Run<TTransportInit, TTransportCreate>(
            string queueName,
            string connectionString,
            int messageCount, int timeOut, int workerCount, LinqMethodTypes linqMethodTypes,
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

                        //create data
                        var producer = new ProducerMethodShared();
                        var id = Guid.NewGuid();
                        if (linqMethodTypes == LinqMethodTypes.Compiled)
                        {
                            producer.RunTestCompiled<TTransportInit>(queueConnection, false, messageCount,
                                logProvider, generateData,
                                verify, false, id, GenerateMethod.CreateNoOpCompiled, 0, oCreation.Scope,
                                false);
                        }
                        else
                        {
                            producer.RunTestDynamic<TTransportInit>(queueConnection, false, messageCount,
                                logProvider, generateData,
                                verify, false, id, GenerateMethod.CreateNoOpDynamic, 0, oCreation.Scope, false);
                        }

                        //process data
                        var consumer = new ConsumerMethodPoisonMessageShared();

                        consumer.RunConsumer<TTransportInit>(queueConnection,
                            false,
                            workerCount,
                            logProvider, timeOut, messageCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35),
                            "second(*%10)", enableChaos, scope);

                        validateErrorCounts(queueName, connectionString, messageCount, scope);
                        verifyQueueCount(queueName, connectionString, oCreation.BaseTransportOptions, scope, messageCount, true, true);

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