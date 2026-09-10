using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.ConsumerAsync
{
    [TestClass]
    public class ConsumerAsyncExpiredMessage
    {
        [TestMethod]
        [DataRow(50, 60, 1, 1, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         DataRow(10, 60, 1, 1, true, IntegrationConnectionInfo.ConnectionTypes.Memory)]
        public void Run(int messageCount, int timeOut, int workerCount, int readerCount, bool enableChaos,
            IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.ConsumerAsyncExpiredMessage();
                consumer.Run<LiteDbMessageQueueInit, FakeMessage, LiteDbMessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString),
                    messageCount, timeOut, workerCount, readerCount, enableChaos,
                    x => Helpers.SetOptions(x, true, true, true),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
            }
        }
    }
}
