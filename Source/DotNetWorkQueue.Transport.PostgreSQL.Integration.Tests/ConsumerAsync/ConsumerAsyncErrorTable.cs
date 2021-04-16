using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.ConsumerAsync
{
    [Collection("consumerasync")]
    public class ConsumerAsyncErrorTable
    {
        [Theory]
        [InlineData(25, 120, 60, 1, 5, false, false),
        InlineData(25, 120, 60, 1, 5, true, false),
        InlineData(5, 120, 60, 1, 5, false, true),
        InlineData(5, 120, 60, 1, 5, true, true)]
        public void Run(int messageCount, int timeOut, int workerCount, 
            int readerCount, int queueSize, bool useTransactions, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.ConsumerAsyncErrorTable();
            consumer.Run<PostgreSqlMessageQueueInit, FakeMessage, PostgreSqlMessageQueueCreation>(queueName,
                ConnectionInfo.ConnectionString,
                messageCount, timeOut, workerCount, readerCount, queueSize, enableChaos, x => Helpers.SetOptions(x,
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
