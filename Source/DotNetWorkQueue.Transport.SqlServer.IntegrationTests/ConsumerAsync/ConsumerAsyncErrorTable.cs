using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.ConsumerAsync
{
    [TestClass]
    public class ConsumerAsyncErrorTable
    {
        [TestMethod]
        [DataRow(1, 60, 1, 1, 0, false, false),
        DataRow(25, 120, 20, 1, 5, false, false),
        DataRow(1, 60, 1, 1, 0, true, false),
        DataRow(25, 120, 20, 1, 5, true, false),
        DataRow(2, 120, 20, 1, 5, false, true),
        DataRow(1, 60, 1, 1, 0, true, true)]
        public void Run(int messageCount, int timeOut, int workerCount,
            int readerCount, int queueSize, bool useTransactions, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.ConsumerAsyncErrorTable();
            consumer.Run<SqlServerMessageQueueInit, FakeMessage, SqlServerMessageQueueCreation>(new QueueConnection(queueName, ConnectionInfo.ConnectionString),
                messageCount, timeOut, workerCount, readerCount, queueSize, enableChaos, x => Helpers.SetOptions(x,
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
