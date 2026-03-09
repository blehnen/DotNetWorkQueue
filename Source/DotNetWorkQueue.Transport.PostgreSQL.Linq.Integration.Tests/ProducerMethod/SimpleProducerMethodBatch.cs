using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.ProducerMethod
{
    [TestClass]
    public class SimpleProducerMethodBatch
    {
        [TestMethod]
        [DataRow(100, true, true, false, false, false, false, false, LinqMethodTypes.Compiled, false),
#if NETFULL
         DataRow(100, true, true, false, false, false, false, false, LinqMethodTypes.Dynamic, false),
         DataRow(100, false, true, false, false, false, false, false, LinqMethodTypes.Dynamic, false),
         DataRow(100, false, false, false, false, false, false, false, LinqMethodTypes.Dynamic, false),
         DataRow(100, true, false, false, false, false, false, false, LinqMethodTypes.Dynamic, false),
         DataRow(100, false, false, false, false, false, true, false, LinqMethodTypes.Dynamic, false),
         DataRow(100, false, true, true, true, true, true, false, LinqMethodTypes.Dynamic, false),
         DataRow(100, false, true, false, true, true, true, false, LinqMethodTypes.Dynamic, false),
         DataRow(100, true, true, false, false, false, false, true, LinqMethodTypes.Dynamic, false),
#endif
         DataRow(100, false, true, false, false, false, false, false, LinqMethodTypes.Compiled, false),
         DataRow(100, false, false, false, false, false, false, false, LinqMethodTypes.Compiled, false),
         DataRow(100, true, false, false, false, false, false, false, LinqMethodTypes.Compiled, false),
         DataRow(100, false, false, false, false, false, true, false, LinqMethodTypes.Compiled, false),
         DataRow(100, false, true, true, true, true, true, false, LinqMethodTypes.Compiled, false),
         DataRow(100, false, true, false, true, true, true, false, LinqMethodTypes.Compiled, false),
         DataRow(100, true, true, false, false, false, false, true, LinqMethodTypes.Compiled, false),

         DataRow(10, false, true, false, false, false, false, false, LinqMethodTypes.Compiled, true),
         DataRow(10, false, false, false, false, false, false, false, LinqMethodTypes.Compiled, true),
         DataRow(10, true, false, false, false, false, false, false, LinqMethodTypes.Compiled, true),
         DataRow(10, false, false, false, false, false, true, false, LinqMethodTypes.Compiled, true),
         DataRow(10, false, true, true, true, true, true, false, LinqMethodTypes.Compiled, true),
         DataRow(50, false, true, false, true, true, true, false, LinqMethodTypes.Compiled, true),
         DataRow(50, true, true, false, false, false, false, true, LinqMethodTypes.Compiled, true)]
        public void Run(
            int messageCount,
            bool interceptors,
            bool enableDelayedProcessing,
            bool enableHoldTransactionUntilMessageCommitted,
            bool enableMessageExpiration,
            bool enablePriority,
            bool enableStatusTable,
            bool additionalColumn,
            LinqMethodTypes linqMethodTypes,
            bool enableChaos)
        {

            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation.SimpleMethodProducer();

            consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(new QueueConnection(queueName,
                    ConnectionInfo.ConnectionString),
                messageCount, linqMethodTypes, interceptors, enableChaos, true, x => Helpers.SetOptions(x,
                    enableDelayedProcessing, !enableHoldTransactionUntilMessageCommitted, enableHoldTransactionUntilMessageCommitted, enableMessageExpiration,
                    enablePriority, !enableHoldTransactionUntilMessageCommitted, enableStatusTable, additionalColumn),
                Helpers.GenerateData, Helpers.Verify);
        }
    }
}
