using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.Route
{
    [Collection("Route")]
    public class RouteTests
    {
        [Theory]
        [InlineData(10, 0, 60, 1, 1, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(20, 0, 180, 1, 2, false, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, 0, 60, 1, 1, true, IntegrationConnectionInfo.ConnectionTypes.Shared)]
        public void Run(int messageCount, int runtime, int timeOut, int readerCount,
           int routeCount, bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var consumer = new DotNetWorkQueue.IntegrationTests.Shared.Route.Implementation.RouteTests();
                consumer.Run<LiteDbMessageQueueInit, LiteDbMessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString),
                    messageCount, runtime, timeOut, readerCount, routeCount, enableChaos, x => { Helpers.SetOptions(x, false, false, true, true); },
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
            }
        }
    }
}
