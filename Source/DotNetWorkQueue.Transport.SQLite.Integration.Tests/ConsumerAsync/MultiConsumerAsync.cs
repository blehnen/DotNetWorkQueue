using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.SQLite.Basic;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.ConsumerAsync
{
    [Collection("Consumer")]
    public class MultiConsumerAsync
    {
        [Theory]
        [InlineData(250, 1, 400, 10, 5, 5, true, false),
         InlineData(100, 0, 180, 10, 5, 0, false, false),
         InlineData(10, 0, 180, 10, 5, 0, false, true)]
        public async Task Run(int messageCount, int runtime, int timeOut, int workerCount,
            int readerCount, int queueSize, bool inMemoryDb, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.
                        MultiConsumerAsync();
                await consumer.Run<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(GetConnections(connectionInfo.ConnectionString),
                    messageCount, runtime, timeOut, workerCount, readerCount, queueSize, enableChaos, x =>
                        Helpers.SetOptions(x,
                            false, true, false,
                            false, true, true, false),
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