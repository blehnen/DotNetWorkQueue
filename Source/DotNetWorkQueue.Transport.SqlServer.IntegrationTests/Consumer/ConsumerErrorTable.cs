using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Consumer
{
    [Collection("Consumer")]
    public class ConsumerErrorTable
    {
        [Theory]
        [InlineData(1, 60, 1, false, false),
         InlineData(10, 60, 5, true, false),
         InlineData(3, 60, 5, true, true)]
        public void Run(int messageCount, int timeOut, int workerCount, bool useTransactions, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer = new DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.ConsumerErrorTable();
            consumer.Run<SqlServerMessageQueueInit, FakeMessage, SqlServerMessageQueueCreation>(new QueueConnection(queueName, ConnectionInfo.ConnectionString),
                messageCount, timeOut, workerCount, enableChaos, x => Helpers.SetOptions(x,
                   true, !useTransactions, useTransactions,
                   false,
                   false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount, ValidateErrorCounts);
        }

        private void ValidateErrorCounts(QueueConnection queueConnection, int arg3, ICreationScope arg4)
        {
            new VerifyErrorCounts(queueConnection).Verify(arg3, 2);
        }

    }
}
