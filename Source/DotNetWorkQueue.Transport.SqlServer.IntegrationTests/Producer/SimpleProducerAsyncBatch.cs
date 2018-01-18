using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.Schema;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Producer
{
    [Collection("SqlServer")]
    public class SimpleProducerAsyncBatch
    {
        [Theory]
        [InlineData(1000, true, true, true, false, false, false, true, false, false),
         InlineData(1000, false, true, true, false, false, false, true, false, false),
         InlineData(1000, false, false, false, false, false, false, false, false, false),
         InlineData(1000, true, false, false, false, false, false, false, false, false),
         InlineData(1000, false, false, false, false, false, false, false, true, false),
         InlineData(1000, false, false, false, false, false, false, true, true, false),
         InlineData(1000, false, true, false, true, true, true, false, true, false),
         InlineData(1000, false, true, true, false, true, true, true, true, false),
            InlineData(1000, true, true, true, false, false, false, true, false, true)]
        public async void Run(
            int messageCount,
            bool interceptors,
            bool enableDelayedProcessing,
            bool enableHeartBeat,
            bool enableHoldTransactionUntilMessageCommitted,
            bool enableMessageExpiration,
            bool enablePriority,
            bool enableStatus,
            bool enableStatusTable,
            bool additionalColumn)
        {

            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (
                var queueCreator =
                    new QueueCreationContainer<SqlServerMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {

                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueName,
                                ConnectionInfo.ConnectionString)
                        )
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
                            oCreation.Options.AdditionalColumns.Add(new Column("OrderID", ColumnTypes.Int, false, null));
                        }

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);

                        var producer = new ProducerAsyncShared();
                        await producer.RunTestAsync<SqlServerMessageQueueInit, FakeMessage>(queueName,
                            ConnectionInfo.ConnectionString, interceptors, messageCount, logProvider,
                            Helpers.GenerateData,
                            Helpers.Verify, true, oCreation.Scope).ConfigureAwait(false);
                    }
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueName,
                                ConnectionInfo.ConnectionString)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }
    }
}
