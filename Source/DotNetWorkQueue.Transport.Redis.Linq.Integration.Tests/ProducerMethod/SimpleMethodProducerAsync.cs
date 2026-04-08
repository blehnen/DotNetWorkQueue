using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.ProducerMethod
{
    [TestClass]
    public class SimpleMethodProducerAsync
    {
        [TestMethod]
        [DataRow(100, true, false),
         DataRow(100, false, false),
         DataRow(100, true, true),
         DataRow(100, false, true)]
        public async Task Run(
           int messageCount,
           bool interceptors,
           bool batchSending)
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = ConnectionInfo.ConnectionString;
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation.SimpleMethodProducerAsync();

            await consumer.Run<RedisQueueInit, RedisQueueCreation>(new QueueConnection(queueName, connectionString),
                messageCount, interceptors, false, batchSending, x => { },
                Helpers.GenerateData, Helpers.Verify);
        }
    }
}
