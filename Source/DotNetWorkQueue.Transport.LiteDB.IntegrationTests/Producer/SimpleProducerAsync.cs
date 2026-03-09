using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.Producer
{
    [TestClass]
    public class SimpleProducerAsync
    {
        [TestMethod]
        [DataRow(1000, true, true, true, IntegrationConnectionInfo.ConnectionTypes.Direct),
         DataRow(100, false, true, true, IntegrationConnectionInfo.ConnectionTypes.Direct),
         DataRow(100, false, false, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         DataRow(100, true, false, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         DataRow(100, false, true, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         DataRow(100, true, true, true, IntegrationConnectionInfo.ConnectionTypes.Direct),

         DataRow(10, true, true, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         DataRow(10, false, true, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         DataRow(10, false, false, false, IntegrationConnectionInfo.ConnectionTypes.Memory),
         DataRow(10, true, false, false, IntegrationConnectionInfo.ConnectionTypes.Memory),
         DataRow(10, false, true, false, IntegrationConnectionInfo.ConnectionTypes.Shared)]
        public async Task Run(
            int messageCount,
            bool interceptors,
            bool enableStatusTable,
            bool enableChaos,
            IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var producer = new DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation.SimpleProducerAsync();
                await producer.Run<LiteDbMessageQueueInit, FakeMessage, LiteDbMessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString),
                    messageCount, interceptors, enableChaos, false, x => x.Options.EnableStatusTable = enableStatusTable,
                    Helpers.GenerateData, Helpers.Verify);
            }
        }
    }
}
