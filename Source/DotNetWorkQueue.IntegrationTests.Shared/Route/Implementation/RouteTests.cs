using System;
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
                    Assert.IsTrue(result.Success, result.ErrorMessage);
                    scope = oCreation.Scope;

                    //Heartbeat settings match the rest of the suite (update every 10s, dead at
                    //30s, swept at 35s) rather than the far tighter 3s/10s/12s used before. This
                    //test runs several consumers against one database, and on SQLite every
                    //heartbeat write queues behind the single write lock along with the dequeue
                    //updates and the deletes. Tight timings there buy nothing - the subject here
                    //is message routing, not the heartbeat - and cost a leftover row when a
                    //commit is still waiting on that lock as the consumer shuts down.
                    var routeTest = new RouteTestsShared();
                    routeTest.RunTest<TTransportInit, FakeMessageA>(queueConnection,
                        true, messageCount, logProvider, generateData, verify, false,
                        GenerateRoutes(routeCount), runtime, timeOut, readerCount, TimeSpan.FromSeconds(30),
                        TimeSpan.FromSeconds(35), oCreation.Scope, "*/10 * * * * *", enableChaos);

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
