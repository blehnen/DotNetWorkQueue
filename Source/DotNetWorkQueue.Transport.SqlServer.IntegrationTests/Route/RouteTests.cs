// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Route;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using System;
using System.Collections.Generic;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Route
{
    [Collection("SqlServer")]
    public class RouteTests
    {
        [Theory]
        [InlineData(10, 1, 60, 1, false, 1),
         InlineData(100, 1, 400, 1, false, 2),
         InlineData(50, 5, 200, 1, false, 3),
         InlineData(10, 5, 180, 1, false, 4),
         InlineData(100, 1, 400, 1, true, 2),
         InlineData(50, 5, 200, 1, true, 2),
         InlineData(10, 5, 180, 1, true, 10),
         InlineData(100, 0, 180, 1, false, 2),
         InlineData(100, 0, 180, 1, true, 2)]
        public void Run(int messageCount, int runtime, int timeOut, int readerCount,
           bool useTransactions, int routeCount)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (var queueCreator =
                new QueueCreationContainer<SqlServerMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {

                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueName,
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

                        var routeTest = new RouteTestsShared();
                        routeTest.RunTest<SqlServerMessageQueueInit, FakeMessageA>(queueName, ConnectionInfo.ConnectionString,
                            true, messageCount, logProvider, Helpers.GenerateData, Helpers.Verify, false,
                            GenerateRoutes(routeCount), runtime, timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), oCreation.Scope);

                        new VerifyQueueRecordCount(queueName, oCreation.Options).Verify(0, false, false);
                    }
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueName,
                                ConnectionInfo.ConnectionString)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }
        private List<string> GenerateRoutes(int routeCount)
        {
            var data = new List<string>();
            for(int i = 1; i <= routeCount; i++)
            {
                data.Add("Route" + i.ToString());
            }
            return data;
        }
    }
}
