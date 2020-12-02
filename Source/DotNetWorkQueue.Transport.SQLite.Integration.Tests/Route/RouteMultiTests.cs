using System;
using System.Collections.Generic;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Route;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Route
{
    [Collection("Route")]
    public class RouteMultiTests
    {
        [Theory]
        [InlineData(100, 0, 400, 1, false, 2, false),
        InlineData(100, 0, 180, 1, true, 2, false),
        InlineData(10, 0, 400, 1, false, 2, true)]
        public void Run(int messageCount, int runtime, int timeOut, int readerCount,
          bool inMemoryDb, int routeCount, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
                    new QueueCreationContainer<SqLiteMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, connectionInfo.ConnectionString);
                    try
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection)
                        )
                        {
                            oCreation.Options.EnableDelayedProcessing = true;
                            oCreation.Options.EnableHeartBeat = true;
                            oCreation.Options.EnableStatus = true;
                            oCreation.Options.EnableStatusTable = true;
                            oCreation.Options.EnableRoute = true;

                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            var routeTest = new RouteMultiTestsShared();
                            routeTest.RunTest<SqLiteMessageQueueInit, FakeMessageA>(queueConnection,
                                true, messageCount, logProvider, Helpers.GenerateData, Helpers.Verify, false,
                                GenerateRoutes(routeCount, 1), GenerateRoutes(routeCount, routeCount + 1), runtime,
                                timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), oCreation.Scope, "second(*%3)", enableChaos);

                            new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options).Verify(0, false, false);
                        }
                    }
                    finally
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection)
                        )
                        {
                            oCreation.RemoveQueue();
                        }
                    }
                }
            }
        }
        private List<string> GenerateRoutes(int routeCount, int seed)
        {
            var data = new List<string>();
            for (var i = seed; i < routeCount + seed; i++)
            {
                data.Add("Route" + i);
            }
            return data;
        }
    }
}
