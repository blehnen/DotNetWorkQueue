using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.ProducerMethod
{
    [TestClass]
    public class SimpleMethodProducerAsyncBatch
    {
        [TestMethod]
        [DataRow(1000, true, true, true, false, false, true, false, false, true, LinqMethodTypes.Dynamic, false),
         DataRow(100, false, true, true, false, false, true, false, false, true, LinqMethodTypes.Dynamic, false),
         DataRow(100, false, false, false, false, false, false, false, false, true, LinqMethodTypes.Dynamic, false),
         DataRow(100, true, false, false, false, false, false, false, false, true, LinqMethodTypes.Dynamic, false),
         DataRow(100, true, true, true, false, false, true, false, false, true, LinqMethodTypes.Compiled, false),
         DataRow(100, false, true, true, false, false, true, false, false, true, LinqMethodTypes.Compiled, false),
         DataRow(100, false, false, false, false, false, false, false, false, true, LinqMethodTypes.Compiled, false),
         DataRow(1000, true, false, false, false, false, false, false, false, true, LinqMethodTypes.Compiled, false),

         DataRow(100, true, true, true, false, false, true, false, false, true, LinqMethodTypes.Dynamic, true),
         DataRow(10, false, true, true, false, false, true, false, false, true, LinqMethodTypes.Dynamic, true),
         DataRow(10, false, false, false, false, false, false, false, false, true, LinqMethodTypes.Dynamic, true),
         DataRow(10, true, false, false, false, false, false, false, false, true, LinqMethodTypes.Dynamic, true),
         DataRow(10, true, true, true, false, false, true, false, false, true, LinqMethodTypes.Compiled, true),
         DataRow(10, false, true, true, false, false, true, false, false, true, LinqMethodTypes.Compiled, true),
         DataRow(10, false, false, false, false, false, false, false, false, true, LinqMethodTypes.Compiled, true),
         DataRow(100, true, false, false, false, false, false, false, false, true, LinqMethodTypes.Compiled, true)]
        public async Task Run(
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
                    new DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation.
                        SimpleMethodProducerAsync();
                await consumer.Run<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(new QueueConnection(queueName, connectionInfo.ConnectionString),
                    messageCount, linqMethodTypes, interceptors, enableChaos, true, x =>
                        Helpers.SetOptions(x,
                            enableDelayedProcessing, enableHeartBeat, enableMessageExpiration, enablePriority, enableStatus,
                            enableStatusTable, additionalColumn, false),
                    Helpers.GenerateData, Helpers.Verify);
            }
        }
    }
}
