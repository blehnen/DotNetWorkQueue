using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.Dashboard.Implementation;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Dashboard
{
    [TestClass]
    public class StaleMessageTimeProviderTests
    {
        [TestMethod]
        public void StaleCutOffComesFromTheConfiguredTimeProvider()
        {
            var queueName = GenerateQueueName.Create();
            var test = new StaleMessageTimeProviderTest();
            test.Run<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(
                new QueueConnection(queueName, ConnectionInfo.ConnectionString),
                x => { });
        }
    }
}
