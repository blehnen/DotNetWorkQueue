using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.Redis.Basic;
using System.Threading.Tasks;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Producer
{
    [Collection("Producer")]
    public class SimpleProducerAsync
    {
        [Theory]
        [InlineData(100, true, false, ConnectionInfoTypes.Linux),
         InlineData(100, false, false, ConnectionInfoTypes.Linux),
         InlineData(250, true, false, ConnectionInfoTypes.Linux),
         InlineData(200, false, false, ConnectionInfoTypes.Linux),
         InlineData(100, true, true, ConnectionInfoTypes.Linux),
         InlineData(100, false, true, ConnectionInfoTypes.Linux)]
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
