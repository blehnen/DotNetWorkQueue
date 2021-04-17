using System;
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Route;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Route
{
    [Collection("Consumer")]
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
                var producer = new DotNetWorkQueue.IntegrationTests.Shared.Route.Implementation.RouteMultiTests();
                producer.Run<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(new QueueConnection(queueName, connectionInfo.ConnectionString), messageCount, runtime, timeOut, readerCount, routeCount,
                    enableChaos, x => Helpers.SetOptions(x,
                        false, true, false, false, true, true, false, true),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
            }
        }
    }
}
