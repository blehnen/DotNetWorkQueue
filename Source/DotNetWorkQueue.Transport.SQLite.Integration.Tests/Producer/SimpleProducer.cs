using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Producer
{
    [TestClass]
    public class SimpleProducer
    {
        [TestMethod]
        [DataRow(1000, true, true, true, false, false, true, false, false, true, false),
         DataRow(100, false, true, true, false, false, true, false, false, true, false),
         DataRow(100, false, false, false, false, false, false, false, false, true, false),
         DataRow(100, true, false, false, false, false, false, false, false, true, false),
         DataRow(100, false, false, false, false, false, false, true, false, true, false),
         DataRow(100, false, false, false, false, false, true, true, false, true, false),
         DataRow(100, false, true, false, true, true, false, true, false, true, false),
         DataRow(100, false, true, true, true, true, true, true, false, true, false),
         DataRow(100, true, true, true, false, false, true, false, true, true, false),

         DataRow(100, true, true, true, false, false, true, false, false, false, false),
         DataRow(100, false, true, true, false, false, true, false, false, false, false),
         DataRow(100, false, false, false, false, false, false, false, false, false, false),
         DataRow(100, true, false, false, false, false, false, false, false, false, false),
         DataRow(100, false, false, false, false, false, false, true, false, false, false),
         DataRow(100, false, false, false, false, false, true, true, false, false, false),
         DataRow(100, false, true, false, true, true, false, true, false, false, false),
         DataRow(100, false, true, true, true, true, true, true, false, false, false),
         DataRow(1000, true, true, true, false, false, true, false, true, false, false),

         DataRow(10, true, true, true, false, false, true, false, false, true, true),
         DataRow(10, false, true, true, false, false, true, false, false, true, true),
         DataRow(10, false, false, false, false, false, false, false, false, true, true),
         DataRow(10, true, false, false, false, false, false, false, false, true, true),
         DataRow(10, false, false, false, false, false, false, true, false, true, true),
         DataRow(10, false, false, false, false, false, true, true, false, true, true),
         DataRow(10, false, true, false, true, true, false, true, false, true, true),
         DataRow(10, false, true, true, true, true, true, true, false, true, true),
         DataRow(10, true, true, true, false, false, true, false, true, true, true),

         DataRow(10, true, true, true, false, false, true, false, false, false, true),
         DataRow(10, false, true, true, false, false, true, false, false, false, true),
         DataRow(10, false, false, false, false, false, false, false, false, false, true),
         DataRow(10, true, false, false, false, false, false, false, false, false, true),
         DataRow(10, false, false, false, false, false, false, true, false, false, true),
         DataRow(10, false, false, false, false, false, true, true, false, false, true),
         DataRow(10, false, true, false, true, true, false, true, false, false, true),
         DataRow(10, false, true, true, true, true, true, true, false, false, true),
         DataRow(10, true, true, true, false, false, true, false, true, false, true)]
        public void Run(
            int messageCount,
            bool interceptors,
            bool enableDelayedProcessing,
            bool enableHeartBeat,
            bool enableMessageExpiration,
            bool enablePriority,
            bool enableStatus,
            bool enableStatusTable,
            bool additionalColumn,
            bool inMemoryDb,
            bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var producer = new DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation.SimpleProducer();
                producer.Run<SqLiteMessageQueueInit, FakeMessage, SqLiteMessageQueueCreation>(new QueueConnection(queueName, connectionInfo.ConnectionString),
                    messageCount, interceptors, enableChaos, false, x => Helpers.SetOptions(x,
                        enableDelayedProcessing, enableHeartBeat, enableMessageExpiration,
                        enablePriority, enableStatus, enableStatusTable, additionalColumn),
                    Helpers.GenerateData, Helpers.Verify);
            }
        }
    }
}
