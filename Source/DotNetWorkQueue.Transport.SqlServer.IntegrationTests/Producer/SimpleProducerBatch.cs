using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.Schema;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Producer
{
    [Collection("Producer")]
    public class SimpleProducerBatch
    {
        [Theory]
        [InlineData(1000, true, true, true, false, false, false, true, false, false, false),
         InlineData(1000, false, true, true, false, false, false, true, false, false, false),
         InlineData(1000, false, false, false, false, false, false, false, false, false, false),
         InlineData(1000, true, false, false, false, false, false, false, false, false, false),
         InlineData(1000, false, false, false, false, false, false, false, true, false, false),
         InlineData(1000, false, false, false, false, false, false, true, true, false, false),
         InlineData(1000, false, true, false, true, true, true, false, true, false, false),
         InlineData(5000, false, true, true, false, true, true, true, true, false, false),
         InlineData(5000, true, true, true, false, false, false, true, false, true, false),

         InlineData(10, true, true, true, false, false, false, true, false, false, true),
         InlineData(10, false, true, true, false, false, false, true, false, false, true),
         InlineData(10, false, false, false, false, false, false, false, false, false, true),
         InlineData(10, true, false, false, false, false, false, false, false, false, true),
         InlineData(10, false, false, false, false, false, false, false, true, false, true),
         InlineData(10, false, false, false, false, false, false, true, true, false, true),
         InlineData(10, false, true, false, true, true, true, false, true, false, true),
         InlineData(50, false, true, true, false, true, true, true, true, false, true),
         InlineData(50, true, true, true, false, false, false, true, false, true, true)]
        public void Run(
            int messageCount,
            bool interceptors,
            bool enableDelayedProcessing,
            bool enableHeartBeat,
            bool enableHoldTransactionUntilMessageCommitted,
            bool enableMessageExpiration,
            bool enablePriority,
            bool enableStatus,
            bool enableStatusTable,
            bool additionalColumn,
            bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var producer = new DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation.SimpleProducer();
            producer.Run<SqlServerMessageQueueInit, FakeMessage, SqlServerMessageQueueCreation>(new QueueConnection(queueName, ConnectionInfo.ConnectionString),
                messageCount, interceptors, enableChaos, true, x => Helpers.SetOptions(x,
                    enableDelayedProcessing, enableHeartBeat, enableHoldTransactionUntilMessageCommitted,
                    enableMessageExpiration,
                    enablePriority, enableStatus, enableStatusTable, additionalColumn),
                Helpers.GenerateData, Helpers.Verify);
        }
    }
}
