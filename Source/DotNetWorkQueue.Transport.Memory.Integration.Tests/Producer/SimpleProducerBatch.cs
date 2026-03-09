using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.Producer
{
    [TestClass]
    public class SimpleProducerBatch
    {
        [TestMethod]
        [DataRow(1000, false),
         DataRow(1000, true)]
        public void Run(
            int messageCount,
            bool interceptors)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var producer = new DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation.SimpleProducer();
                producer.Run<MemoryMessageQueueInit, FakeMessage, MessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString),
                    messageCount, interceptors, false, true, x => { },
                    Helpers.GenerateData, Helpers.Verify);
            }
        }
    }
}
