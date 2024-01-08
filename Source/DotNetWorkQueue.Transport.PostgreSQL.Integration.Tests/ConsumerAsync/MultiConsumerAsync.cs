using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.ConsumerAsync
{
    [Collection("consumerasyncmulti")]
    public class MultiConsumerAsync
    {
        [Theory]
        [InlineData(250, 1, 90, 10, 5, 5, false, false),
         InlineData(250, 1, 90, 10, 5, 5, true, false),
         InlineData(100, 0, 90, 10, 5, 0, false, false),
         InlineData(100, 0, 90, 10, 5, 0, true, false),
         InlineData(25, 1, 90, 10, 5, 5, true, true),
         InlineData(10, 0, 90, 10, 5, 0, false, true)]
        public async Task Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            bool useTransactions, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.MultiConsumerAsync();
            await consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(GetConnections(ConnectionInfo.ConnectionString),
                messageCount, runtime, timeOut, workerCount, readerCount, queueSize, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions, false,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);

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