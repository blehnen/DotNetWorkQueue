using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.Consumer
{
    [TestClass]
    public class ConsumerExpiredMessage
    {
        [TestMethod]
        [DataRow(50, 0, 60, 1, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         DataRow(10, 0, 60, 1, true, IntegrationConnectionInfo.ConnectionTypes.Memory)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.ConsumerExpiredMessage();
                consumer.Run<LiteDbMessageQueueInit, FakeMessage, LiteDbMessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString),
                    messageCount, runtime, timeOut, workerCount, enableChaos, x => Helpers.SetOptions(x, true, true, true),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
            }
        }
    }
}
