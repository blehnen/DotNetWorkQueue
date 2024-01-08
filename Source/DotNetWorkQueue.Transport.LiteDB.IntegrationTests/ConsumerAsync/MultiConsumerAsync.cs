using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.ConsumerAsync
{
    [Collection("ConsumerAsync")]
    public class MultiConsumerAsync
    {
        [Theory]
        [InlineData(100, 1, 400, 10, 1, 5, false, IntegrationConnectionInfo.ConnectionTypes.Shared),
         InlineData(50, 0, 180, 10, 1, 0, false, IntegrationConnectionInfo.ConnectionTypes.Shared),
         InlineData(10, 0, 180, 10, 1, 0, true, IntegrationConnectionInfo.ConnectionTypes.Shared)]
        public async Task Run(int messageCount, int runtime, int timeOut, int workerCount,
            int readerCount, int queueSize, bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.MultiConsumerAsync();
                await consumer.Run<LiteDbMessageQueueInit, LiteDbMessageQueueCreation>(GetConnections(connectionInfo.ConnectionString),
                messageCount, runtime, timeOut, workerCount, readerCount, queueSize, enableChaos, x => Helpers.SetOptions(x, false, false, true),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
            }
        }
        private List<QueueConnection> GetConnections(string connectionString)
        {
            var list = new List<QueueConnection>(3);
            var connection = new QueueConnection(GenerateQueueName.Create(), connectionString);
            list.Add(connection);
            connection = new QueueConnection(GenerateQueueName.Create(), connectionString);
            list.Add(connection);
            connection = new QueueConnection(GenerateQueueName.Create(), connectionString);
            list.Add(connection);
            return list;
        }
    }
}