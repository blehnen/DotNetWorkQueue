using System;
using System.Collections.Generic;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Route;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Route
{
    [Collection("Redis")]
    public class RouteTests
    {
        [Theory]
        [InlineData(10, 1, 60, 1, 1, ConnectionInfoTypes.Windows, false),
         InlineData(100, 1, 400, 1, 2, ConnectionInfoTypes.Windows, false),
         InlineData(50, 5, 200, 1, 3, ConnectionInfoTypes.Windows, false),
         InlineData(10, 5, 180, 1, 4, ConnectionInfoTypes.Windows, false),
         InlineData(100, 1, 400, 1, 2, ConnectionInfoTypes.Windows, false),
         InlineData(50, 5, 200, 1, 2, ConnectionInfoTypes.Linux, false),
         InlineData(10, 5, 180, 1, 10, ConnectionInfoTypes.Linux, false),
         InlineData(100, 0, 180, 1, 2, ConnectionInfoTypes.Linux, false),
         InlineData(500, 0, 180, 1, 2, ConnectionInfoTypes.Linux, true)]
        public void Run(int messageCount, int runtime, int timeOut, int readerCount,
           int routeCount, ConnectionInfoTypes type, bool batch)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (var queueCreator =
                new QueueCreationContainer<RedisQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {

                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<RedisQueueCreation>(queueName,
                                connectionString)
                        )
                    {
                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);

                        var routeTest = new RouteTestsShared();
                        routeTest.RunTest<RedisQueueInit, FakeMessageA>(queueName, connectionString,
                            true, messageCount, logProvider, Helpers.GenerateData, Helpers.Verify, batch,
                            GenerateRoutes(routeCount), runtime, timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12));

                        using (var count = new VerifyQueueRecordCount(queueName, connectionString))
                        {
                            count.Verify(0, false, -1);
                        }
                    }
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<RedisQueueCreation>(queueName,
                                connectionString)
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
