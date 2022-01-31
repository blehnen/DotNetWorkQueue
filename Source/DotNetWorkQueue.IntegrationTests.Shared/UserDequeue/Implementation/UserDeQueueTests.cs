using System;
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.UserDequeue.Implementation
{
    public class UserDequeueTests
    {
        public void Run<TTransportInit, TTransportCreate>(
            QueueConnection queueConnection,
            int messageCount, int runtime, int timeOut, int readerCount,
            int valueCount, bool enableChaos,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, int, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, int, ICreationScope> verify,
            Action<QueueConnection, IBaseTransportOptions, ICreationScope, int, bool, bool> verifyQueueCount,
            Action<QueueConsumerConfiguration, int> setQueueOptions)
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

                    var test = new UserDeQueueTestsShared();
                    test.RunTest<TTransportInit, FakeMessageA>(queueConnection,
                        true, messageCount, logProvider, generateData, verify, false,
                        GenerateUserData(valueCount), runtime, timeOut, readerCount, TimeSpan.FromSeconds(10),
                        TimeSpan.FromSeconds(12), oCreation.Scope, "second(*%3)", enableChaos, setQueueOptions);

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

        private List<int> GenerateUserData(int count)
        {
            var data = new List<int>();
            for (var i = 1; i <= count; i++)
            {
                data.Add(i);
            }
            return data;
        }
    }
}
