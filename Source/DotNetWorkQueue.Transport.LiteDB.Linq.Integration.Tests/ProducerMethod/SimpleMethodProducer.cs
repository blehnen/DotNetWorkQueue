using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.ProducerMethod
{
    [Collection("Consumer")]
    public class SimpleMethodProducer
    {
        [Theory]
        [InlineData(1000, true, true, true, false, LinqMethodTypes.Dynamic, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, false, true, true, false,  LinqMethodTypes.Dynamic, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, false, false, false, false,   LinqMethodTypes.Dynamic, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, true, false, false, false, LinqMethodTypes.Dynamic, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, true, true, true, false,   LinqMethodTypes.Compiled, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, false, true, true, false,  LinqMethodTypes.Compiled, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, false, false, false, false,   LinqMethodTypes.Compiled, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(1000, true, false, false, false,  LinqMethodTypes.Compiled, false, IntegrationConnectionInfo.ConnectionTypes.Direct),

         InlineData(100, true, true, true, false,  LinqMethodTypes.Dynamic, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, false, true, true, false,   LinqMethodTypes.Dynamic, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, false, false, false, false,   LinqMethodTypes.Dynamic, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, true, false, false, true,  LinqMethodTypes.Dynamic, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, true, true, true, true,  LinqMethodTypes.Compiled, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, false, true, true, false,   LinqMethodTypes.Compiled, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, false, false, false, false, LinqMethodTypes.Compiled, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(100, true, false, false, false,  LinqMethodTypes.Compiled, true, IntegrationConnectionInfo.ConnectionTypes.Shared)]
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
                consumer.Run<LiteDbMessageQueueInit, LiteDbMessageQueueCreation>(queueName,
                    connectionInfo.ConnectionString,
                    messageCount, linqMethodTypes, interceptors, enableChaos, false, x => Helpers.SetOptions(x, enableDelayedProcessing, enableMessageExpiration, enableStatusTable),
                    Helpers.GenerateData, Helpers.Verify);
            }
        }
    }
}
