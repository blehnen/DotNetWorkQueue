using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.Dashboard.Implementation;
using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Dashboard
{
    [TestClass]
    [Retry(1)]
    public class StaleMessageTimeProviderTests
    {
        [TestMethod]
        public void StaleCutOffComesFromTheConfiguredTimeProvider()
        {
            var queueName = GenerateQueueName.Create();
            var test = new StaleMessageTimeProviderTest();
            test.Run<RedisQueueInit, RedisQueueCreation>(
                new QueueConnection(queueName, ConnectionInfo.ConnectionString),
                x => { });
        }
    }
}
