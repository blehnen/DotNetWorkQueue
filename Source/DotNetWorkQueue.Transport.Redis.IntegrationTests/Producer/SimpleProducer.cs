using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Producer
{
    [Collection("Producer")]
    public class SimpleProducer
    {
        [Theory]
        [InlineData(100, true, false, false, false, ConnectionInfoTypes.Linux),
         InlineData(100, false, false, false, false, ConnectionInfoTypes.Linux),
         InlineData(500, true, false, false, false, ConnectionInfoTypes.Linux),
         InlineData(500, false, false, false, false, ConnectionInfoTypes.Linux),
         InlineData(100, true, true, false, false, ConnectionInfoTypes.Linux),
         InlineData(100, false, true, false, false, ConnectionInfoTypes.Linux),
         InlineData(100, true, false, true, false, ConnectionInfoTypes.Linux),
         InlineData(100, false, false, false, true, ConnectionInfoTypes.Linux),
         InlineData(100, true, false, true, true, ConnectionInfoTypes.Linux),
         InlineData(100, true, true, true, false, ConnectionInfoTypes.Linux),
         InlineData(100, false, true, true, true, ConnectionInfoTypes.Linux)]
        public void Run(
            int messageCount,
            bool interceptors,
            bool batchSending,
            bool enableDelay,
            bool enableExpiration,
            ConnectionInfoTypes type)
        {

            var queueName = GenerateQueueName.Create();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            var producer = new DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation.SimpleProducer();
            if (enableExpiration && enableDelay)
            {
                producer.Run<RedisQueueInit, FakeMessage, RedisQueueCreation>(new QueueConnection(queueName, connectionString),
                    messageCount, interceptors, false, batchSending,x => { },
                    Helpers.GenerateDelayExpiredData, Helpers.Verify);
            }
            else if (enableDelay)
            {
                producer.Run<RedisQueueInit, FakeMessage, RedisQueueCreation>(new QueueConnection(queueName, connectionString),
                    messageCount, interceptors, false, batchSending,x => { },
                    Helpers.GenerateDelayData, Helpers.Verify);
            }
            else if (enableExpiration)
            {
                producer.Run<RedisQueueInit, FakeMessage, RedisQueueCreation>(new QueueConnection(queueName, connectionString),
                    messageCount, interceptors, false, batchSending,x => { },
                    Helpers.GenerateExpiredData, Helpers.Verify);
            }
            else
            {
                producer.Run<RedisQueueInit, FakeMessage, RedisQueueCreation>(new QueueConnection(queueName, connectionString),
                    messageCount, interceptors, false, batchSending,x => { },
                    Helpers.GenerateData, Helpers.Verify);
            }
        }
    }
}
