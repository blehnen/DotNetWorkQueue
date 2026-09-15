using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Dashboard.Implementation;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.Dashboard
{
    [TestClass]
    [Retry(1)]
    public class StaleMessageTimeProviderTests
    {
        [TestMethod]
        public void StaleCutOffComesFromTheConfiguredTimeProvider()
        {
            using (var connectionInfo = new IntegrationConnectionInfo(IntegrationConnectionInfo.ConnectionTypes.Direct))
            {
                var queueName = GenerateQueueName.Create();
                var test = new StaleMessageTimeProviderTest();
                test.Run<LiteDbMessageQueueInit, LiteDbMessageQueueCreation>(
                    new QueueConnection(queueName, connectionInfo.ConnectionString),
                    x => { });
            }
        }
    }
}
