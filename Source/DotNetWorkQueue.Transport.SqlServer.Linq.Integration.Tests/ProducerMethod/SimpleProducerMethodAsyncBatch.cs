using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.ProducerMethod
{
    [Collection("Producer")]
    public class SimpleProducerMethodAsyncBatch
    {
        [Theory]
        [InlineData(1000, true, true, false, false, false, false, false, LinqMethodTypes.Compiled, false),
#if NETFULL
        InlineData(1000, true, true, false, false, false, false, false, LinqMethodTypes.Dynamic, false),
         InlineData(1000, false, true, false, false, false, false, false, LinqMethodTypes.Dynamic, false),
         InlineData(1000, false, false, false, false, false, false, false, LinqMethodTypes.Dynamic, false),
         InlineData(1000, true, false, false, false, false, false, false, LinqMethodTypes.Dynamic, false),
         InlineData(1000, false, false, false, false, false, true, false, LinqMethodTypes.Dynamic, false),
        InlineData(1000, false, true, true, true, true, true, false, LinqMethodTypes.Dynamic, false),
         InlineData(1000, false, true, false, true, true, true, false, LinqMethodTypes.Dynamic, false),
         InlineData(1000, true, true, false, false, false, false, true, LinqMethodTypes.Dynamic, false),
#endif      
         InlineData(1000, false, true, false, false, false, false, false, LinqMethodTypes.Compiled, false),
         InlineData(1000, false, false, false, false, false, false, false, LinqMethodTypes.Compiled, false),
         InlineData(1000, true, false, false, false, false, false, false, LinqMethodTypes.Compiled, false),
         InlineData(1000, false, false, false, false, false, true, false, LinqMethodTypes.Compiled, false),
        InlineData(1000, false, true, true, true, true, true, false, LinqMethodTypes.Compiled, false),
         InlineData(1000, false, true, false, true, true, true, false, LinqMethodTypes.Compiled, false),
         InlineData(1000, true, true, false, false, false, false, true, LinqMethodTypes.Compiled, false),

         InlineData(100, false, true, false, false, false, false, false, LinqMethodTypes.Compiled, true),
         InlineData(100, false, false, false, false, false, false, false, LinqMethodTypes.Compiled, true),
         InlineData(100, true, false, false, false, false, false, false, LinqMethodTypes.Compiled, true),
         InlineData(100, false, false, false, false, false, true, false, LinqMethodTypes.Compiled, true),
        InlineData(100, false, true, true, true, true, false, false, LinqMethodTypes.Compiled, true),
         InlineData(100, false, true, false, true, true, true, false, LinqMethodTypes.Compiled, true),
         InlineData(100, true, true, false, false, false, false, true, LinqMethodTypes.Compiled, true)]
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
