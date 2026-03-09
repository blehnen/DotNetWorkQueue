using System;
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Route;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Route
{
    [TestClass]
    public class RouteTests
    {
        [TestMethod]
        [DataRow(100, 0, 180, 1, false, 2, false),
         DataRow(100, 0, 180, 1, true, 2, false),
         DataRow(10, 0, 180, 1, false, 2, true)]
        public void Run(int messageCount, int runtime, int timeOut, int readerCount,
           bool useTransactions, int routeCount, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer = new DotNetWorkQueue.IntegrationTests.Shared.Route.Implementation.RouteTests();
            consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(new QueueConnection(queueName,
                    ConnectionInfo.ConnectionString),
                messageCount, runtime, timeOut, readerCount, routeCount, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions, false,
                    false, !useTransactions, true, false, true),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
        }
    }
}
