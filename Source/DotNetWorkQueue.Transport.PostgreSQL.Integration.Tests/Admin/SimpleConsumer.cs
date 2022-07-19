using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Admin
{
    [Collection("consumeradmin")]
    public class SimpleConsumer
    {
        [Theory]
        [InlineData(10, 10, 120, 10, false),
         InlineData(50, 10, 200, 10, true),
         InlineData(20, 10, 240, 15, false),
         InlineData(20, 10, 240, 15, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, bool useTransactions)
        {
            var queueName = GenerateQueueName.Create();
            var consumer = new DotNetWorkQueue.IntegrationTests.Shared.Admin.Implementation.SimpleConsumerAdmin();
            consumer.Run<PostgreSqlMessageQueueInit, FakeMessage, PostgreSqlMessageQueueCreation>(new QueueConnection(queueName,
                    ConnectionInfo.ConnectionString),
                messageCount, runtime, timeOut, workerCount, false, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions, false,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
        }
    }
}
