using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.Redis.Basic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Producer
{
    [TestClass]
    public class SimpleProducerAsync
    {
        [TestMethod]
        [DataRow(100, true, false, ConnectionInfoTypes.Linux),
         DataRow(100, false, false, ConnectionInfoTypes.Linux),
         DataRow(250, true, false, ConnectionInfoTypes.Linux),
         DataRow(200, false, false, ConnectionInfoTypes.Linux),
         DataRow(100, true, true, ConnectionInfoTypes.Linux),
         DataRow(100, false, true, ConnectionInfoTypes.Linux)]
        public async Task Run(
            int messageCount,
            bool interceptors,
            bool batchSending,
            ConnectionInfoTypes type)
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            var producer = new DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation.SimpleProducerAsync();
            await producer.Run<RedisQueueInit, FakeMessage, RedisQueueCreation>(new QueueConnection(queueName, connectionString),
                messageCount, interceptors, false, batchSending, x => { },
                Helpers.GenerateData, Helpers.Verify);
        }
    }
}
