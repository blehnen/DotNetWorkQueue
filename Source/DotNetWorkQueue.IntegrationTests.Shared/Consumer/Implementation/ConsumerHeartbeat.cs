using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation
{
    public class ConsumerHeartbeat
    {
        public void Run<TTransportInit, TMessage, TTransportCreate>(
            QueueConnection queueConnection,
            int messageCount, int runtime, int timeOut, int workerCount, bool enableChaos,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify,
            Action<QueueConnection, IBaseTransportOptions, ICreationScope, int, bool, bool> verifyQueueCount,
            TimeSpan? heartBeatTimeOverride = null,
            TimeSpan? heartBeatMonitorTimeOverride = null,
            TimeSpan? heartBeatUpdateTimeOverride = null)
            where TTransportInit : ITransportInit, new()
            where TMessage : class
            where TTransportCreate : class, IQueueCreation
        {

            var heartBeatTime = heartBeatTimeOverride ?? TimeSpan.FromSeconds(10);
            var heartBeatMonitorTime = heartBeatMonitorTimeOverride ?? TimeSpan.FromSeconds(12);
            var heartBeatUpdateTime = heartBeatUpdateTimeOverride ?? TimeSpan.FromSeconds(3);

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
                    Assert.IsTrue(result.Success, result.ErrorMessage);
                    scope = oCreation.Scope;

                    var producer = new ProducerShared();
                    producer.RunTest<TTransportInit, TMessage>(queueConnection, false, messageCount,
                        logProvider, generateData,
                        verify, false, oCreation.Scope, false);

                    //The claim window has to clear the slowest single heartbeat write this transport
                    //can produce, and still sit below runtime - above runtime the handler finishes
                    //before a claim could ever lapse and the scenario stops testing heartbeats at all.
                    //See the caller for why SQLite needs a wider window than the default (GitHub #328).
                    var consumer = new ConsumerHeartBeatShared<TMessage>();
                    consumer.RunConsumer<TTransportInit>(queueConnection,
                        false,
                        logProvider,
                        runtime, messageCount,
                        workerCount, timeOut, heartBeatTime, heartBeatMonitorTime, heartBeatUpdateTime,
                        null, enableChaos, scope);

                    verifyQueueCount(queueConnection, oCreation.BaseTransportOptions, scope, 0, false, false);

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
