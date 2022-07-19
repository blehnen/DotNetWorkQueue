using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Integration.Tests.Admin
{
    [Collection("Consumeradmin")]
    public class SimpleConsumer
    {
        [Theory]
        [InlineData(10, 10, 60, 5, false),
        InlineData(10, 10, 30, 10, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, bool useTransactions)
        {
            var queueName = GenerateQueueName.Create();
            var consumer = new DotNetWorkQueue.IntegrationTests.Shared.Admin.Implementation.SimpleConsumerAdmin();
            consumer.Run<SqlServerMessageQueueInit, FakeMessage, SqlServerMessageQueueCreation>(new QueueConnection(queueName, ConnectionInfo.ConnectionString),
                messageCount, runtime, timeOut, workerCount, false, x => Helpers.SetOptions(x,
                    false, !useTransactions, useTransactions,
                    false,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
        }
    }
}
