using System;
using System.Collections.Generic;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Route;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Route
{
    [Collection("postgresql")]
    public class RouteMultiTests
    {
        [Theory]
        [InlineData(100, 1, 400, 1, true, 2),
        InlineData(100, 0, 180, 1, false, 2)]
        public void Run(int messageCount, int runtime, int timeOut, int readerCount,
          bool useTransactions, int routeCount)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (var queueCreator =
                new QueueCreationContainer<PostgreSqlMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {

                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueName,
                                ConnectionInfo.ConnectionString)
                        )
                    {
                        oCreation.Options.EnableDelayedProcessing = true;
                        oCreation.Options.EnableHeartBeat = !useTransactions;
                        oCreation.Options.EnableHoldTransactionUntilMessageCommitted = useTransactions;
                        oCreation.Options.EnableStatus = !useTransactions;
                        oCreation.Options.EnableStatusTable = true;
                        oCreation.Options.EnableRoute = true;

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);

                        var routeTest = new RouteMultiTestsShared();
                        routeTest.RunTest<PostgreSqlMessageQueueInit, FakeMessageA>(queueName, ConnectionInfo.ConnectionString,
                            true, messageCount, logProvider, Helpers.GenerateData, Helpers.Verify, false,
                            GenerateRoutes(routeCount, 1), GenerateRoutes(routeCount, routeCount + 1), runtime, timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), oCreation.Scope, "second(*%3)");

                        new VerifyQueueRecordCount(queueName, oCreation.Options).Verify(0, false, false);
                    }
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueName,
                                ConnectionInfo.ConnectionString)
                        )
                    {
                        oCreation.RemoveQueue();
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
