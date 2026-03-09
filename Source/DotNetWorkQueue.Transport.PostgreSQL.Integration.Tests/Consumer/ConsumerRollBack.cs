using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Consumer
{
    [TestClass]
    public class ConsumerRollBack
    {

        [TestMethod]
        [DataRow(500, 0, 60, 5, false, false),
        DataRow(50, 5, 90, 10, false, false),
        DataRow(500, 0, 60, 5, true, false),
        DataRow(50, 5, 90, 10, true, false),
        DataRow(5, 5, 90, 10, false, true),
        DataRow(50, 0, 60, 5, true, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, bool useTransactions, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer = new DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.ConsumerRollBack();
            consumer.Run<PostgreSqlMessageQueueInit, FakeMessage, PostgreSqlMessageQueueCreation>(new QueueConnection(queueName,
                    ConnectionInfo.ConnectionString),
                messageCount, runtime, timeOut, workerCount, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions, false,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
        }
    }
}
