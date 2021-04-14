using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.Schema;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Producer
{
    [Collection("Producer")]
    public class SimpleProducer
    {
        [Theory]
        [InlineData(1000, true, true, true, false, false, false, true, false, false, false),
         InlineData(1000, false, true, true, false, false, false, true, false, false, false),
         InlineData(1000, false, false, false, false, false, false, false, false, false, false),
         InlineData(1000, true, false, false, false, false, false, false, false, false, false),
         InlineData(1000, false, false, false, false, false, false, false, true, false, false),
         InlineData(1000, false, false, false, false, false, false, true, true, false, false),
         InlineData(1000, false, true, false, true, true, true, false, true, false, false),
         InlineData(1000, false, true, true, false, true, true, true, true, false, false),
         InlineData(1000, true, true, true, false, false, false, true, false, true, false),

         InlineData(10, true, true, true, false, false, false, true, false, false, true),
         InlineData(10, false, true, true, false, false, false, true, false, false, true),
         InlineData(10, false, false, false, false, false, false, false, false, false, true),
         InlineData(10, true, false, false, false, false, false, false, false, false, true),
         InlineData(10, false, false, false, false, false, false, false, true, false, true),
         InlineData(10, false, false, false, false, false, false, true, true, false, true),
         InlineData(10, false, true, false, true, true, true, false, true, false, true),
         InlineData(10, false, true, true, false, true, true, true, true, false, true),
         InlineData(10, true, true, true, false, false, false, true, false, true, true)]
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
            producer.Run<SqlServerMessageQueueInit, FakeMessage, SqlServerMessageQueueCreation>(queueName,
                ConnectionInfo.ConnectionString,
                messageCount, interceptors, enableChaos, false, x => SetOptions(x,
                    enableDelayedProcessing, enableHeartBeat, enableHoldTransactionUntilMessageCommitted,
                    enableMessageExpiration,
                    enablePriority, enableStatus, enableStatusTable, additionalColumn),
                Helpers.GenerateData, Helpers.Verify);
        }
        private void SetOptions(SqlServerMessageQueueCreation oCreation, bool enableDelayedProcessing,
            bool enableHeartBeat,
            bool enableHoldTransactionUntilMessageCommitted,
            bool enableMessageExpiration,
            bool enablePriority,
            bool enableStatus,
            bool enableStatusTable,
            bool additionalColumn)
        {
            oCreation.Options.EnableDelayedProcessing = enableDelayedProcessing;
            oCreation.Options.EnableHeartBeat = enableHeartBeat;
            oCreation.Options.EnableMessageExpiration = enableMessageExpiration;
            oCreation.Options.EnableHoldTransactionUntilMessageCommitted =
                enableHoldTransactionUntilMessageCommitted;
            oCreation.Options.EnablePriority = enablePriority;
            oCreation.Options.EnableStatus = enableStatus;
            oCreation.Options.EnableStatusTable = enableStatusTable;

            if (additionalColumn)
            {
                oCreation.Options.AdditionalColumns.Add(new Column("OrderID", ColumnTypes.Int, true, null));
            }
        }
    }
}
