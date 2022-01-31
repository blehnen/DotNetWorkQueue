using System;
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Route;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Route
{
    [Collection("Route")]
    public class RouteMultiTests
    {
        [Theory]
        [InlineData(100, 0, 400, 1, false, 2, false),
        InlineData(10, 0, 400, 1, true, 5, true)]
        public void Run(int messageCount, int runtime, int timeOut, int readerCount,
          bool useTransactions, int routeCount, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.Route.Implementation.RouteMultiTests();
            consumer.Run<SqlServerMessageQueueInit, SqlServerMessageQueueCreation>(new QueueConnection(queueName, ConnectionInfo.ConnectionString),
                messageCount, runtime, timeOut, readerCount, routeCount, enableChaos, x => Helpers.SetOptions(x,
                   true, !useTransactions, useTransactions,
                   false,
                   false, !useTransactions, true, false, true),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);

        }
    }
}
