using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using DotNetWorkQueue.Transport.SqlServer.Schema;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.ProducerMethod
{
    [Collection("Producer")]
    public class SimpleProducerMethodAsyncBatch
    {
        [Theory]
        [InlineData(1000, true, true, true, false, false, false, true, false, false, LinqMethodTypes.Compiled, false),
#if NETFULL
        InlineData(1000, true, true, true, false, false, false, true, false, false, LinqMethodTypes.Dynamic, false),
         InlineData(1000, false, true, true, false, false, false, true, false, false, LinqMethodTypes.Dynamic, false),
         InlineData(1000, false, false, false, false, false, false, false, false, false, LinqMethodTypes.Dynamic, false),
         InlineData(1000, true, false, false, false, false, false, false, false, false, LinqMethodTypes.Dynamic, false),
         InlineData(1000, false, false, false, false, false, false, false, true, false, LinqMethodTypes.Dynamic, false),
         InlineData(1000, false, false, false, false, false, false, true, true, false, LinqMethodTypes.Dynamic, false),
         InlineData(1000, false, true, false, true, true, true, false, true, false, LinqMethodTypes.Dynamic, false),
         InlineData(1000, false, true, true, false, true, true, true, true, false, LinqMethodTypes.Dynamic, false),
         InlineData(1000, true, true, true, false, false, false, true, false, true, LinqMethodTypes.Dynamic, false),
#endif      
         InlineData(1000, false, true, true, false, false, false, true, false, false, LinqMethodTypes.Compiled, false),
         InlineData(1000, false, false, false, false, false, false, false, false, false, LinqMethodTypes.Compiled, false),
         InlineData(1000, true, false, false, false, false, false, false, false, false, LinqMethodTypes.Compiled, false),
         InlineData(1000, false, false, false, false, false, false, false, true, false, LinqMethodTypes.Compiled, false),
         InlineData(1000, false, false, false, false, false, false, true, true, false, LinqMethodTypes.Compiled, false),
         InlineData(1000, false, true, false, true, true, true, false, true, false, LinqMethodTypes.Compiled, false),
         InlineData(1000, false, true, true, false, true, true, true, true, false, LinqMethodTypes.Compiled, false),
         InlineData(1000, true, true, true, false, false, false, true, false, true, LinqMethodTypes.Compiled, false),

         InlineData(100, false, true, true, false, false, false, true, false, false, LinqMethodTypes.Compiled, true),
         InlineData(100, false, false, false, false, false, false, false, false, false, LinqMethodTypes.Compiled, true),
         InlineData(100, true, false, false, false, false, false, false, false, false, LinqMethodTypes.Compiled, true),
         InlineData(100, false, false, false, false, false, false, false, true, false, LinqMethodTypes.Compiled, true),
         InlineData(100, false, false, false, false, false, false, true, true, false, LinqMethodTypes.Compiled, true),
         InlineData(100, false, true, false, true, true, true, false, true, false, LinqMethodTypes.Compiled, true),
         InlineData(100, false, true, true, false, true, true, true, true, false, LinqMethodTypes.Compiled, true),
         InlineData(100, true, true, true, false, false, false, true, false, true, LinqMethodTypes.Compiled, true)]
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
            LinqMethodTypes linqMethodTypes, bool enableChaos)
        {

            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (
                var queueCreator =
                    new QueueCreationContainer<SqlServerMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, ConnectionInfo.ConnectionString);
                try
                {

                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection)
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

                        var id = Guid.NewGuid();
                        var producer = new ProducerMethodAsyncShared();
                        await producer.RunTestAsync<SqlServerMessageQueueInit>(queueConnection, interceptors, messageCount, logProvider,
                            Helpers.GenerateData,
                            Helpers.Verify, true, 0, id, linqMethodTypes, oCreation.Scope, enableChaos).ConfigureAwait(false);
                    }
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }
    }
}
