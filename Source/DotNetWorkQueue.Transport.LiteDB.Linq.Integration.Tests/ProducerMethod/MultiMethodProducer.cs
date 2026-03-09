using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.IntegrationTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.ProducerMethod
{
    [TestClass]
    public class MultiMethodProducer
    {
        [TestMethod]
        [DataRow(100, LinqMethodTypes.Dynamic, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         DataRow(10, LinqMethodTypes.Dynamic, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         DataRow(10, LinqMethodTypes.Compiled, true, IntegrationConnectionInfo.ConnectionTypes.Shared),
         DataRow(100, LinqMethodTypes.Compiled, false, IntegrationConnectionInfo.ConnectionTypes.Direct)]
        public void Run(int messageCount, LinqMethodTypes linqMethodTypes, bool enableChaos,
            IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation.MultiMethodProducer();
                consumer.Run<LiteDbMessageQueueInit, LiteDbMessageQueueCreation>(new QueueConnection(queueName,
                    connectionInfo.ConnectionString),
                    messageCount, 10, linqMethodTypes, enableChaos,
                    Helpers.GenerateData, VerifyQueueCount);
            }
        }

        private void VerifyQueueCount(QueueConnection arg1, IBaseTransportOptions arg2, ICreationScope arg3, int arg4,
            string arg5)
        {
            new VerifyQueueData(arg1, (LiteDbMessageQueueTransportOptions)arg2, arg3).Verify(arg4, arg5);
        }
    }
}
