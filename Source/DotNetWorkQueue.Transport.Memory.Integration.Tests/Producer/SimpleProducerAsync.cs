using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.Memory.Basic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.Producer
{
    [TestClass]
    public class SimpleProducerAsync
    {
        [TestMethod]
        [DataRow(1000, true),
         DataRow(1000, false)]
        public async Task Run(
            int messageCount,
            bool interceptors)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var producer = new DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation.SimpleProducerAsync();
                await producer.Run<MemoryMessageQueueInit, FakeMessage, MessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString),
                    messageCount, interceptors, false, false, x => { },
                    Helpers.GenerateData, Helpers.Verify);
            }
        }
    }
}
