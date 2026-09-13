using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Dashboard.Implementation;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Dashboard
{
    [TestClass]
    public class StaleMessageTimeProviderTests
    {
        [TestMethod]
        public void StaleCutOffComesFromTheConfiguredTimeProvider()
        {
            using (var connectionInfo = new IntegrationConnectionInfo(false))
            {
                var queueName = GenerateQueueName.Create();
                var test = new StaleMessageTimeProviderTest();
                test.Run<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                    new QueueConnection(queueName, connectionInfo.ConnectionString),
                    x => { });
            }
        }
    }
}
