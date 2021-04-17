using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation
{
    public class MultiMethodProducer
    {
        public void Run<TTransportInit, TTransportCreate>(
            QueueConnection queueConnection,
            int messageCount, int queueCount, LinqMethodTypes linqMethodTypes, bool enableChaos,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, IBaseTransportOptions, ICreationScope, int, string> verifyQueueCount)
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
                    var result = oCreation.CreateQueue();
                    Assert.True(result.Success, result.ErrorMessage);
                    scope = oCreation.Scope;

                    RunTest<TTransportInit>(queueConnection, messageCount, queueCount, logProvider, linqMethodTypes, oCreation.Scope,
                        enableChaos, generateData);
                    LoggerShared.CheckForErrors(queueConnection.Queue);
                    verifyQueueCount(queueConnection, oCreation.BaseTransportOptions, scope, messageCount * queueCount, null);

                }
                finally
                {

                    oCreation.RemoveQueue();
                    oCreation.Dispose();
                    scope?.Dispose();

                }
            }
        }

        private void RunTest<TTransportInit>(QueueConnection queueConnection, 
            int messageCount, int queueCount, ILogger logProvider, LinqMethodTypes linqMethodTypes, ICreationScope scope, bool enableChaos,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData)
            where TTransportInit : ITransportInit, new()
        {
            var tasks = new List<Task>(queueCount);
            for (var i = 0; i < queueCount; i++)
            {
                var id = Guid.NewGuid();
                var producer = new ProducerMethodShared();
                if (linqMethodTypes == LinqMethodTypes.Compiled)
                {
                    tasks.Add(new Task(() => producer.RunTestCompiled<TTransportInit>(queueConnection, false, messageCount,
                        logProvider, generateData, Verify,  true, false, id, GenerateMethod.CreateCompiled, 0, scope, enableChaos)));
                }
                else
                {
                    tasks.Add(new Task(() => producer.RunTestDynamic<TTransportInit>(queueConnection, false, messageCount,
                        logProvider, generateData, Verify, true, false, id, GenerateMethod.CreateDynamic, 0, scope, enableChaos)));
                }
            }
            tasks.AsParallel().ForAll(x => x.Start());
            Task.WaitAll(tasks.ToArray());
        }

        private void Verify(QueueConnection arg1, QueueProducerConfiguration arg2, long arg3, ICreationScope arg4)
        {
            //no-op at single producer level
        }
    }
}
