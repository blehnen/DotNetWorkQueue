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
    public class ConsumerAsyncPoisonMessage
    {
        [TestMethod]
        [DataRow(1, 60, 1, 1, 0, false, false),
         DataRow(10, 60, 5, 1, 0, false, false),
         DataRow(10, 60, 5, 1, 0, true, false),
         DataRow(50, 60, 20, 2, 2, true, false),
         DataRow(3, 60, 5, 1, 0, false, true),
         DataRow(3, 60, 5, 1, 0, true, true)]
        public void Run(int messageCount, int timeOut, int workerCount, int readerCount, int queueSize,
            bool useTransactions, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.ConsumerAsyncPoisonMessage();
            consumer.Run<SqlServerMessageQueueInit, FakeMessage, SqlServerMessageQueueCreation>(new QueueConnection(queueName, ConnectionInfo.ConnectionString),
                messageCount, timeOut, workerCount, readerCount, queueSize, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions,
                    false,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount, ValidateErrorCounts);
        }

        private void ValidateErrorCounts(QueueConnection queueConnection, int arg3, ICreationScope arg4)
        {
            //poison messages are moved to the error queue right away
            //they don't update the tracking table, so specify 0 for the error count.
            //They still update the error table itself
            new VerifyErrorCounts(queueConnection).Verify(arg3, 0);
        }
    }
}
