using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Producer
{
    [TestClass]
    public class SimpleProducer
    {
        [TestMethod]
        [DataRow(100, true, false, false, false),
         DataRow(100, false, false, false, false),
         DataRow(500, true, false, false, false),
         DataRow(500, false, false, false, false),
         DataRow(100, true, true, false, false),
         DataRow(100, false, true, false, false),
         DataRow(100, true, false, true, false),
         DataRow(100, false, false, false, true),
         DataRow(100, true, false, true, true),
         DataRow(100, true, true, true, false),
         DataRow(100, false, true, true, true)]
        public void Run(
            int messageCount,
            bool interceptors,
            bool batchSending,
            bool enableDelay,
            bool enableExpiration)
        {

            var queueName = GenerateQueueName.Create();
            var connectionString = ConnectionInfo.ConnectionString;
            var producer = new DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation.SimpleProducer();
            if (enableExpiration && enableDelay)
            {
                producer.Run<RedisQueueInit, FakeMessage, RedisQueueCreation>(new QueueConnection(queueName, connectionString),
                    messageCount, interceptors, false, batchSending, x => { },
                    Helpers.GenerateDelayExpiredData, Helpers.Verify);
            }
            else if (enableDelay)
            {
                producer.Run<RedisQueueInit, FakeMessage, RedisQueueCreation>(new QueueConnection(queueName, connectionString),
                    messageCount, interceptors, false, batchSending, x => { },
                    Helpers.GenerateDelayData, Helpers.Verify);
            }
            else if (enableExpiration)
            {
                producer.Run<RedisQueueInit, FakeMessage, RedisQueueCreation>(new QueueConnection(queueName, connectionString),
                    messageCount, interceptors, false, batchSending, x => { },
                    Helpers.GenerateExpiredData, Helpers.Verify);
            }
            else
            {
                producer.Run<RedisQueueInit, FakeMessage, RedisQueueCreation>(new QueueConnection(queueName, connectionString),
                    messageCount, interceptors, false, batchSending, x => { },
                    Helpers.GenerateData, Helpers.Verify);
            }
        }
    }
}
