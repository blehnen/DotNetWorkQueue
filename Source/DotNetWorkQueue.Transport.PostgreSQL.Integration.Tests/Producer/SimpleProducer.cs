using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Producer
{
    [Collection("postgresql")]
    public class SimpleProducer
    {
        [Theory]
        [InlineData(100, true, true, true, false, false, false, true, false, false, false),
         InlineData(100, false, true, true, false, false, false, true, false, false, false),
         InlineData(100, false, false, false, false, false, false, false, false, false, false),
         InlineData(100, true, false, false, false, false, false, false, false, false, false),
         InlineData(100, false, false, false, false, false, false, false, true, false, false),
         InlineData(100, false, false, false, false, false, false, true, true, false, false),
         InlineData(100, false, true, false, true, true, true, false, true, false, false),
         InlineData(100, false, true, true, false, true, true, true, true, false, false),
         InlineData(100, true, true, true, false, false, false, true, false, true, false),

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
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (var queueCreator =
                new QueueCreationContainer<PostgreSqlMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {

                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueName,
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
                            oCreation.Options.AdditionalColumns.Add(new Column("OrderID", ColumnTypes.Integer, true));
                        }

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);

                        var producer = new ProducerShared();
                        producer.RunTest<PostgreSqlMessageQueueInit, FakeMessage>(queueName,
                            ConnectionInfo.ConnectionString, interceptors, messageCount, logProvider,
                            Helpers.GenerateData,
                            Helpers.Verify, false, oCreation.Scope, enableChaos);
                    }
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueName,
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
