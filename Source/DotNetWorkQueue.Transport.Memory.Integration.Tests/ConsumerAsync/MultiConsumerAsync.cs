using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.Memory.Basic;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.ConsumerAsync
{
    [TestClass]
    public class MultiConsumerAsync
    {
        [TestMethod]
        [DataRow(10, 5, 65, 10, 1, 2),
         DataRow(10, 8, 60, 7, 1, 1),
         DataRow(100, 0, 30, 10, 5, 0)]
        public async Task Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.MultiConsumerAsync();
                await consumer.Run<MemoryMessageQueueInit, MessageQueueCreation>(GetConnections(connectionInfo.ConnectionString),
                    messageCount, runtime, timeOut, workerCount, readerCount, queueSize, false, x => { },
                    Helpers.GenerateData, Helpers.Verify, VerifyQueueCount);
            }
        }

        private List<QueueConnection> GetConnections(string connectionString)
        {
            var list = new List<QueueConnection>(3);
            var connection = new QueueConnection(GenerateQueueName.Create(), connectionString);
            list.Add(connection);
            connection = new QueueConnection(GenerateQueueName.Create(), connectionString);
            list.Add(connection);
            connection = new QueueConnection(GenerateQueueName.Create(), connectionString);
            list.Add(connection);
            return list;
        }
        private void VerifyQueueCount(QueueConnection queueConnection, IBaseTransportOptions arg3, ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            //noop
        }
    }
}