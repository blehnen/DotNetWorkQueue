using System;
using System.Collections.Generic;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Route;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.Route
{
    [Collection("Route")]
    public class RouteMultiTests
    {
        [Theory]
        [InlineData(100, 0, 400, 1,  2, false, IntegrationConnectionInfo.ConnectionTypes.Memory),
        InlineData(100, 0, 180, 1, 2, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
        InlineData(10, 0, 400, 1,  2, true, IntegrationConnectionInfo.ConnectionTypes.Shared)]
        public void Run(int messageCount, int runtime, int timeOut, int readerCount,
         int routeCount, bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
                    new QueueCreationContainer<LiteDbMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, connectionInfo.ConnectionString);
                    ICreationScope scope = null;
                    var oCreation = queueCreator.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection);
                    try
                    {
                        oCreation.Options.EnableStatusTable = true;
                        oCreation.Options.EnableRoute = true;

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);
                        scope = oCreation.Scope;

                        var routeTest = new RouteMultiTestsShared();
                        routeTest.RunTest<LiteDbMessageQueueInit, FakeMessageA>(queueConnection,
                            true, messageCount, logProvider, Helpers.GenerateData, Helpers.Verify, false,
                            GenerateRoutes(routeCount, 1), GenerateRoutes(routeCount, routeCount + 1), runtime,
                            timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), oCreation.Scope,
                            "second(*%3)", enableChaos);

                        new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options, scope)
                            .Verify(0, false, false);
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
