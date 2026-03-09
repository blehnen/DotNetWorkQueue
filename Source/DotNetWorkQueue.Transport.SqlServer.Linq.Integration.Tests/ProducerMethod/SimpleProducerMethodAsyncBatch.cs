using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.ProducerMethod
{
    [TestClass]
    public class SimpleProducerMethodAsyncBatch
    {
        [TestMethod]
        [DataRow(1000, true, true, false, false, false, false, false, LinqMethodTypes.Compiled, false),
#if NETFULL
        DataRow(1000, true, true, false, false, false, false, false, LinqMethodTypes.Dynamic, false),
         DataRow(1000, false, true, false, false, false, false, false, LinqMethodTypes.Dynamic, false),
         DataRow(1000, false, false, false, false, false, false, false, LinqMethodTypes.Dynamic, false),
         DataRow(1000, true, false, false, false, false, false, false, LinqMethodTypes.Dynamic, false),
         DataRow(1000, false, false, false, false, false, true, false, LinqMethodTypes.Dynamic, false),
        DataRow(1000, false, true, true, true, true, true, false, LinqMethodTypes.Dynamic, false),
         DataRow(1000, false, true, false, true, true, true, false, LinqMethodTypes.Dynamic, false),
         DataRow(1000, true, true, false, false, false, false, true, LinqMethodTypes.Dynamic, false),
#endif      
         DataRow(1000, false, true, false, false, false, false, false, LinqMethodTypes.Compiled, false),
         DataRow(1000, false, false, false, false, false, false, false, LinqMethodTypes.Compiled, false),
         DataRow(1000, true, false, false, false, false, false, false, LinqMethodTypes.Compiled, false),
         DataRow(1000, false, false, false, false, false, true, false, LinqMethodTypes.Compiled, false),
        DataRow(1000, false, true, true, true, true, true, false, LinqMethodTypes.Compiled, false),
         DataRow(1000, false, true, false, true, true, true, false, LinqMethodTypes.Compiled, false),
         DataRow(1000, true, true, false, false, false, false, true, LinqMethodTypes.Compiled, false),

         DataRow(100, false, true, false, false, false, false, false, LinqMethodTypes.Compiled, true),
         DataRow(100, false, false, false, false, false, false, false, LinqMethodTypes.Compiled, true),
         DataRow(100, true, false, false, false, false, false, false, LinqMethodTypes.Compiled, true),
         DataRow(100, false, false, false, false, false, true, false, LinqMethodTypes.Compiled, true),
        DataRow(100, false, true, true, true, true, false, false, LinqMethodTypes.Compiled, true),
         DataRow(100, false, true, false, true, true, true, false, LinqMethodTypes.Compiled, true),
         DataRow(100, true, true, false, false, false, false, true, LinqMethodTypes.Compiled, true)]
        public async Task Run(
            int messageCount,
            bool interceptors,
            bool enableDelayedProcessing,
            bool enableHoldTransactionUntilMessageCommitted,
            bool enableMessageExpiration,
            bool enablePriority,
            bool enableStatusTable,
            bool additionalColumn,
            LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation.SimpleMethodProducerAsync();
            await consumer.Run<SqlServerMessageQueueInit, SqlServerMessageQueueCreation>(new QueueConnection(queueName, ConnectionInfo.ConnectionString),
                messageCount, linqMethodTypes, interceptors, enableChaos, true, x => Helpers.SetOptions(x,
                    enableDelayedProcessing, !enableHoldTransactionUntilMessageCommitted, enableHoldTransactionUntilMessageCommitted,
                    enableMessageExpiration,
                    enablePriority, !enableHoldTransactionUntilMessageCommitted, enableStatusTable, additionalColumn),
                Helpers.GenerateData, Helpers.Verify);
        }
    }
}
