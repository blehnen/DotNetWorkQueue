using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.ConsumerMethod
{
    [TestClass]
    public class ConsumerMethodExpiredMessage
    {
        [TestMethod]
        [DataRow(100, 0, 60, 5, false, false),
         DataRow(100, 5, 120, 5, true, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount,
            bool useTransactions, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation.
                    ConsumerMethodExpiredMessage();
            consumer.Run<SqlServerMessageQueueInit, SqlServerMessageQueueCreation>(new QueueConnection(queueName, ConnectionInfo.ConnectionString),
                messageCount, runtime, timeOut, workerCount, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions,
                    true,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
        }
    }
}
