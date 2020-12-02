using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Producer
{
    [Collection("producer")]
    public class SimpleProducerAsyncBatch
    {
        [Theory]
        [InlineData(500, true, true, true, false, false, false, true, false, false, false),
         InlineData(500, false, true, true, false, false, false, true, false, false, false),
         InlineData(500, false, false, false, false, false, false, false, false, false, false),
         InlineData(500, true, false, false, false, false, false, false, false, false, false),
         InlineData(500, false, false, false, false, false, false, false, true, false, false),
         InlineData(500, false, false, false, false, false, false, true, true, false, false),
         InlineData(500, false, true, false, true, true, true, false, true, false, false),
         InlineData(500, false, true, true, false, true, true, true, true, false, false),
         InlineData(1000, true, true, true, false, false, false, true, false, true, false),

         InlineData(50, true, true, true, false, false, false, true, false, false, true),
         InlineData(50, false, true, true, false, false, false, true, false, false, true),
         InlineData(50, false, false, false, false, false, false, false, false, false, true),
         InlineData(50, true, false, false, false, false, false, false, false, false, true),
         InlineData(50, false, false, false, false, false, false, false, true, false, true),
         InlineData(50, false, false, false, false, false, false, true, true, false, true),
         InlineData(50, false, true, false, true, true, true, false, true, false, true),
         InlineData(50, false, true, true, false, true, true, true, true, false, true),
         InlineData(100, true, true, true, false, false, false, true, false, true, true)]
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
            bool additionalColumn,
            bool enableChaos)
        {

            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (
                var queueCreator =
                    new QueueCreationContainer<PostgreSqlMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);
                try
                {

                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection)
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
                            oCreation.Options.AdditionalColumns.Add(new Column("OrderID", ColumnTypes.Integer, false));
                        }

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);

                        var producer = new ProducerAsyncShared();
                        await producer.RunTestAsync<PostgreSqlMessageQueueInit, FakeMessage>(queueConnection, interceptors, messageCount, logProvider,
                            Helpers.GenerateData,
                            Helpers.Verify, true, oCreation.Scope, enableChaos).ConfigureAwait(false);
                    }
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }
    }
}
