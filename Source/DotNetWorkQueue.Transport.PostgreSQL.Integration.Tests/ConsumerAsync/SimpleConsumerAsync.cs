using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.ConsumerAsync
{
    [TestClass]
    public class SimpleConsumerAsync
    {
        [TestMethod]
        [DataRow(500, 1, 400, 10, 5, 5, false, 1, false),
         DataRow(500, 1, 400, 10, 5, 5, true, 1, false),
         DataRow(500, 0, 180, 10, 5, 0, false, 1, false),
         DataRow(500, 0, 180, 10, 5, 0, true, 1, false),
         DataRow(50, 0, 180, 10, 5, 0, false, 1, true),
         DataRow(50, 0, 180, 10, 5, 0, true, 1, true)]
        public async Task Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            bool useTransactions, int messageType, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.SimpleConsumerAsync();
            await consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(new QueueConnection(queueName,
                    ConnectionInfo.ConnectionString),
                messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions, false,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
        }
    }
}
