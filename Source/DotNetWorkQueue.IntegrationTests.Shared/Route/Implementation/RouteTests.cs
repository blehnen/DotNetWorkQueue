using System;
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.Route.Implementation
{
    public class RouteTests
    {
        public void Run<TTransportInit, TTransportCreate>(
            QueueConnection queueConnection,
            int messageCount, int runtime, int timeOut, int readerCount,
            int routeCount, bool enableChaos,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, string, ICreationScope> verify,
            Action<QueueConnection, IBaseTransportOptions, ICreationScope, int, bool, bool> verifyQueueCount)
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

                    var routeTest = new RouteTestsShared();
                    routeTest.RunTest<TTransportInit, FakeMessageA>(queueConnection,
                        true, messageCount, logProvider, generateData, verify, false,
                        GenerateRoutes(routeCount), runtime, timeOut, readerCount, TimeSpan.FromSeconds(10),
                        TimeSpan.FromSeconds(12), oCreation.Scope, "second(*%3)", enableChaos);

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

        private List<string> GenerateRoutes(int routeCount)
        {
            var data = new List<string>();
            for (var i = 1; i <= routeCount; i++)
            {
                data.Add("Route" + i);
            }
            return data;
        }
    }
}
