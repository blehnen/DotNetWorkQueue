using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.IntegrationTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.ProducerMethod
{
    [TestClass]
    public class SimpleMethodProducerBatch
    {
        [TestMethod]
        [DataRow(1000, true, true, true, false, LinqMethodTypes.Dynamic, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         DataRow(100, false, true, true, false, LinqMethodTypes.Dynamic, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         DataRow(100, false, false, false, false, LinqMethodTypes.Dynamic, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         DataRow(100, true, false, false, false, LinqMethodTypes.Dynamic, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         DataRow(100, true, true, true, false, LinqMethodTypes.Compiled, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         DataRow(100, false, true, true, false, LinqMethodTypes.Compiled, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         DataRow(100, false, false, false, false, LinqMethodTypes.Compiled, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         DataRow(1000, true, false, false, false, LinqMethodTypes.Compiled, false, IntegrationConnectionInfo.ConnectionTypes.Direct),

         DataRow(100, true, true, true, false, LinqMethodTypes.Dynamic, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         DataRow(10, false, true, true, false, LinqMethodTypes.Dynamic, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         DataRow(10, false, false, false, false, LinqMethodTypes.Dynamic, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         DataRow(10, true, false, false, false, LinqMethodTypes.Dynamic, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         DataRow(10, true, true, true, false, LinqMethodTypes.Compiled, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         DataRow(10, false, true, true, false, LinqMethodTypes.Compiled, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         DataRow(10, false, false, false, false, LinqMethodTypes.Compiled, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         DataRow(100, true, false, false, false, LinqMethodTypes.Compiled, true, IntegrationConnectionInfo.ConnectionTypes.Shared)]
        public void Run(
            int messageCount,
            bool interceptors,
            bool enableDelayedProcessing,
            bool enableMessageExpiration,
            bool enableStatusTable,
            LinqMethodTypes linqMethodTypes,
            bool enableChaos,
            IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation.SimpleMethodProducer();
                consumer.Run<LiteDbMessageQueueInit, LiteDbMessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString),
                    messageCount, linqMethodTypes, interceptors, enableChaos, true, x => Helpers.SetOptions(x, enableDelayedProcessing, enableMessageExpiration, enableStatusTable),
                    Helpers.GenerateData, Helpers.Verify);
            }
        }
    }
}
