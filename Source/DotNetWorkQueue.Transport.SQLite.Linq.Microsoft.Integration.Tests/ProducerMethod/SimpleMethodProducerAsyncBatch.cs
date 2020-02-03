using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using DotNetWorkQueue.Transport.SQLite.Microsoft.Basic;
using DotNetWorkQueue.Transport.SQLite.Microsoft.Integration.Tests;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using DotNetWorkQueue.Transport.SQLite.Shared.Schema;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Microsoft.Integration.Tests.ProducerMethod
{
    [Collection("Producer")]
    public class SimpleMethodProducerAsyncBatch
    {
        [Theory]
        [InlineData(100, true, true, true, false, false, true, false, false, true, LinqMethodTypes.Compiled, false),
         InlineData(100, false, true, true, false, false, true, false, false, true, LinqMethodTypes.Compiled, false),
         InlineData(10, false, false, false, false, false, false, false, false, true, LinqMethodTypes.Compiled, true),
         InlineData(10, true, false, false, false, false, false, false, false, true, LinqMethodTypes.Compiled, true)]
        public async void Run(
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
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (
                    var queueCreator =
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
                                oCreation.Options.AdditionalColumns.Add(new Column("OrderID", ColumnTypes.Integer, false, null));
                            }

                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            var producer = new ProducerMethodAsyncShared();
                            var id = Guid.NewGuid();
                            await producer.RunTestAsync<SqLiteMessageQueueInit>(queueName,
                                connectionInfo.ConnectionString, interceptors, messageCount, logProvider,
                                Helpers.GenerateData,
                                Helpers.Verify, true, 0, id, linqMethodTypes, oCreation.Scope, enableChaos).ConfigureAwait(false);
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
