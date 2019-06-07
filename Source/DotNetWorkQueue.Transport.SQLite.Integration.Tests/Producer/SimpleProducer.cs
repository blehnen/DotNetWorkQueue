using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using DotNetWorkQueue.Transport.SQLite.Shared.Schema;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Producer
{
    [Collection("SQLite")]
    public class SimpleProducer
    {
        [Theory]
        [InlineData(1000, true, true, true, false, false, true, false, false, true, false),
         InlineData(100, false, true, true, false, false, true, false, false, true, false),
         InlineData(100, false, false, false, false, false, false, false, false, true, false),
         InlineData(100, true, false, false, false, false, false, false, false, true, false),
         InlineData(100, false, false, false, false, false, false, true, false, true, false),
         InlineData(100, false, false, false, false, false, true, true, false, true, false),
         InlineData(100, false, true, false, true, true, false, true, false, true, false),
         InlineData(100, false, true, true, true, true, true, true, false, true, false),
         InlineData(100, true, true, true, false, false, true, false, true, true, false),

         InlineData(100, true, true, true, false, false, true, false, false, false, false),
         InlineData(100, false, true, true, false, false, true, false, false, false, false),
         InlineData(100, false, false, false, false, false, false, false, false, false, false),
         InlineData(100, true, false, false, false, false, false, false, false, false, false),
         InlineData(100, false, false, false, false, false, false, true, false, false, false),
         InlineData(100, false, false, false, false, false, true, true, false, false, false),
         InlineData(100, false, true, false, true, true, false, true, false, false, false),
         InlineData(100, false, true, true, true, true, true, true, false, false, false),
         InlineData(1000, true, true, true, false, false, true, false, true, false, false),

         InlineData(10, true, true, true, false, false, true, false, false, true, true),
         InlineData(10, false, true, true, false, false, true, false, false, true, true),
         InlineData(10, false, false, false, false, false, false, false, false, true, true),
         InlineData(10, true, false, false, false, false, false, false, false, true, true),
         InlineData(10, false, false, false, false, false, false, true, false, true, true),
         InlineData(10, false, false, false, false, false, true, true, false, true, true),
         InlineData(10, false, true, false, true, true, false, true, false, true, true),
         InlineData(10, false, true, true, true, true, true, true, false, true, true),
         InlineData(10, true, true, true, false, false, true, false, true, true, true),

         InlineData(10, true, true, true, false, false, true, false, false, false, true),
         InlineData(10, false, true, true, false, false, true, false, false, false, true),
         InlineData(10, false, false, false, false, false, false, false, false, false, true),
         InlineData(10, true, false, false, false, false, false, false, false, false, true),
         InlineData(10, false, false, false, false, false, false, true, false, false, true),
         InlineData(10, false, false, false, false, false, true, true, false, false, true),
         InlineData(10, false, true, false, true, true, false, true, false, false, true),
         InlineData(10, false, true, true, true, true, true, true, false, false, true),
         InlineData(10, true, true, true, false, false, true, false, true, false, true)]
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
            bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
                    new QueueCreationContainer<SqLiteMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    try
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
                            )
                        {
                            oCreation.Options.EnableDelayedProcessing = enableDelayedProcessing;
                            oCreation.Options.EnableHeartBeat = enableHeartBeat;
                            oCreation.Options.EnableMessageExpiration = enableMessageExpiration;
                            oCreation.Options.EnablePriority = enablePriority;
                            oCreation.Options.EnableStatus = enableStatus;
                            oCreation.Options.EnableStatusTable = enableStatusTable;

                            if (additionalColumn)
                            {
                                oCreation.Options.AdditionalColumns.Add(new Column("OrderID", ColumnTypes.Integer, true, null));
                            }

                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            var producer = new ProducerShared();
                            producer.RunTest<SqLiteMessageQueueInit, FakeMessage>(queueName,
                                connectionInfo.ConnectionString, interceptors, messageCount, logProvider,
                                Helpers.GenerateData,
                                Helpers.Verify, false, oCreation.Scope, enableChaos);
                        }
                    }
                    finally
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
                            )
                        {
                            oCreation.RemoveQueue();
                        }
                    }
                }
            }
        }
    }
}
