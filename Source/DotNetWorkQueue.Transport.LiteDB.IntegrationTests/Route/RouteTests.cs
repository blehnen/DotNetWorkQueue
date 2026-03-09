using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.Route
{
    [TestClass]
    public class RouteTests
    {
        [TestMethod]
        [DataRow(10, 0, 60, 1, 1, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         DataRow(20, 0, 180, 1, 2, false, IntegrationConnectionInfo.ConnectionTypes.Memory),
         DataRow(10, 0, 60, 1, 1, true, IntegrationConnectionInfo.ConnectionTypes.Shared)]
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
