using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Consumer
{
    [Collection("consumer")]
    public class ConsumerErrorTable
    {
        [Theory]
        [InlineData(100, 120, 40, false, false),
         InlineData(100, 120, 40, true, false),
         InlineData(10, 100, 5, true, false),
         InlineData(10, 120, 40, true, true),
         InlineData(1, 100, 5, false, true)]
        public void Run(int messageCount, int timeOut, int workerCount, bool useTransactions, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer = new DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.ConsumerErrorTable();
            consumer.Run<PostgreSqlMessageQueueInit, FakeMessage, PostgreSqlMessageQueueCreation>(queueName,
                ConnectionInfo.ConnectionString,
                messageCount, timeOut, workerCount, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions, false,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount, ValidateErrorCounts);
        }

        private void ValidateErrorCounts(string arg1, string arg2, int arg3, ICreationScope arg4)
        {
            new VerifyErrorCounts(arg1).Verify(arg3, 2);
        }
    }
}
