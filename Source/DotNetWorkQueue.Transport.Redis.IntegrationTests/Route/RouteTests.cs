using System;
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Route;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Route
{
    [Collection("Route")]
    public class RouteTests
    {
        [Theory]
        [InlineData(100, 0, 180, 1, 2, ConnectionInfoTypes.Linux)]
        public void Run(int messageCount, int runtime, int timeOut, int readerCount,
           int routeCount, ConnectionInfoTypes type)
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            var consumer = new DotNetWorkQueue.IntegrationTests.Shared.Route.Implementation.RouteTests();
            consumer.Run<RedisQueueInit, RedisQueueCreation>(queueName,
                connectionString,
                messageCount, runtime, timeOut, readerCount, routeCount, false, x => { },
                Helpers.GenerateData, Helpers.Verify, VerifyQueueCount);
        }

        private void VerifyQueueCount(string arg1, string arg2, IBaseTransportOptions arg3, ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            using (var count = new VerifyQueueRecordCount(arg1, arg2))
            {
                count.Verify(0, false, -1);
            }
        }
    }
}
