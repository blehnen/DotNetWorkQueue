﻿using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation
{
    public class ConsumerErrorTable
    {
        public void Run<TTransportInit, TMessage, TTransportCreate>(
            string queueName,
            string connectionString,
            int messageCount, int timeOut, int workerCount, bool enableChaos,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            Action<string, string, IBaseTransportOptions, ICreationScope, int, bool, bool> verifyQueueCount,
            Action<string, string, int, ICreationScope> validateErrorCounts)
            where TTransportInit : ITransportInit, new()
            where TMessage : class
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
                    var producer = new ProducerShared();
                    producer.RunTest<TTransportInit, TMessage>(queueConnection, false, messageCount,
                        logProvider, generateData,
                        verify, false, oCreation.Scope, false);

                    //process data
                    var consumer = new ConsumerErrorShared<TMessage>();
                    consumer.RunConsumer<TTransportInit>(queueConnection,
                        false,
                        logProvider,
                        workerCount, timeOut, messageCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35),
                        "second(*%10)", null, enableChaos, scope);

                    validateErrorCounts(queueName, connectionString, messageCount, scope);
                    verifyQueueCount(queueName, connectionString, oCreation.BaseTransportOptions, scope,
                        messageCount, true, false);

                    consumer.PurgeErrorMessages<TTransportInit>(queueConnection,
                        false, logProvider, false, scope);

                    //table should not be empty yet
                    validateErrorCounts(queueName, connectionString, messageCount, scope);

                    //purge error records
                    consumer.PurgeErrorMessages<TTransportInit>(queueConnection,
                        false, logProvider, true, scope);

                    //table should be empty now
                    validateErrorCounts(queueName, connectionString, 0, scope);

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