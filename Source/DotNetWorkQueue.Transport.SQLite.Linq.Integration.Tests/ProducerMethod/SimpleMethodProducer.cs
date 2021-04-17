using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using DotNetWorkQueue.Transport.SQLite.Schema;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.ProducerMethod
{
    [Collection("Producer")]
    public class SimpleMethodProducer
    {
        [Theory]
        [InlineData(1000, true, true, true, false, false, true, false, false, true, LinqMethodTypes.Dynamic, false),
         InlineData(100, false, true, true, false, false, true, false, false, true, LinqMethodTypes.Dynamic, false),
         InlineData(100, false, false, false, false, false, false, false, false, true, LinqMethodTypes.Dynamic, false),
         InlineData(100, true, false, false, false, false, false, false, false, true, LinqMethodTypes.Dynamic, false),
         InlineData(100, true, true, true, false, false, true, false, false, true, LinqMethodTypes.Compiled, false),
         InlineData(100, false, true, true, false, false, true, false, false, true, LinqMethodTypes.Compiled, false),
         InlineData(100, false, false, false, false, false, false, false, false, true, LinqMethodTypes.Compiled, false),
         InlineData(1000, true, false, false, false, false, false, false, false, true, LinqMethodTypes.Compiled, false),

         InlineData(100, true, true, true, false, false, true, false, false, true, LinqMethodTypes.Dynamic, true),
         InlineData(10, false, true, true, false, false, true, false, false, true, LinqMethodTypes.Dynamic, true),
         InlineData(10, false, false, false, false, false, false, false, false, true, LinqMethodTypes.Dynamic, true),
         InlineData(10, true, false, false, false, false, false, false, false, true, LinqMethodTypes.Dynamic, true),
         InlineData(10, true, true, true, false, false, true, false, false, true, LinqMethodTypes.Compiled, true),
         InlineData(10, false, true, true, false, false, true, false, false, true, LinqMethodTypes.Compiled, true),
         InlineData(10, false, false, false, false, false, false, false, false, true, LinqMethodTypes.Compiled, true),
         InlineData(100, true, false, false, false, false, false, false, false, true, LinqMethodTypes.Compiled, true)]
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
            LinqMethodTypes linqMethodTypes,
            bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation.SimpleMethodProducer();
                consumer.Run<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(new QueueConnection(queueName, connectionInfo.ConnectionString),
                    messageCount, linqMethodTypes, interceptors, enableChaos, false, x =>
                        Helpers.SetOptions(x,
                            enableDelayedProcessing, enableHeartBeat, enableMessageExpiration, enablePriority, enableStatus,
                            enableStatusTable, additionalColumn, false),
                    Helpers.GenerateData, Helpers.Verify);
            }
        }
    }
}
