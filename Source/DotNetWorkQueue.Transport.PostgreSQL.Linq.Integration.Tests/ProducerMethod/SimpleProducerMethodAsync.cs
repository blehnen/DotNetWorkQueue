using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.ProducerMethod
{
    [Collection("producer")]
    public class SimpleProducerMethodAsync
    {
        [Theory]
        [InlineData(100, true, true, true, false, false, false, true, false, false, LinqMethodTypes.Compiled, false),
#if NETFULL
         InlineData(100, true, true, true, false, false, false, true, false, false, LinqMethodTypes.Dynamic, false),
         InlineData(100, false, true, true, false, false, false, true, false, false, LinqMethodTypes.Dynamic, false),
         InlineData(100, false, false, false, false, false, false, false, false, false, LinqMethodTypes.Dynamic, false),
         InlineData(100, true, false, false, false, false, false, false, false, false, LinqMethodTypes.Dynamic, false),
         InlineData(100, false, false, false, false, false, false, false, true, false, LinqMethodTypes.Dynamic, false),
         InlineData(100, false, false, false, false, false, false, true, true, false, LinqMethodTypes.Dynamic, false),
         InlineData(100, false, true, false, true, true, true, false, true, false, LinqMethodTypes.Dynamic, false),
         InlineData(100, false, true, true, false, true, true, true, true, false, LinqMethodTypes.Dynamic, false),
         InlineData(100, true, true, true, false, false, false, true, false, true, LinqMethodTypes.Dynamic, false),
#endif       
         InlineData(100, false, true, true, false, false, false, true, false, false, LinqMethodTypes.Compiled, false),
         InlineData(100, false, false, false, false, false, false, false, false, false, LinqMethodTypes.Compiled, false),
         InlineData(100, true, false, false, false, false, false, false, false, false, LinqMethodTypes.Compiled, false),
         InlineData(100, false, false, false, false, false, false, false, true, false, LinqMethodTypes.Compiled, false),
         InlineData(100, false, false, false, false, false, false, true, true, false, LinqMethodTypes.Compiled, false),
         InlineData(100, false, true, false, true, true, true, false, true, false, LinqMethodTypes.Compiled, false),
         InlineData(100, false, true, true, false, true, true, true, true, false, LinqMethodTypes.Compiled, false),
         InlineData(100, true, true, true, false, false, false, true, false, true, LinqMethodTypes.Compiled, false),

         InlineData(10, false, true, true, false, false, false, true, false, false, LinqMethodTypes.Compiled, true),
         InlineData(10, false, false, false, false, false, false, false, false, false, LinqMethodTypes.Compiled, true),
         InlineData(10, true, false, false, false, false, false, false, false, false, LinqMethodTypes.Compiled, true),
         InlineData(10, false, false, false, false, false, false, false, true, false, LinqMethodTypes.Compiled, true),
         InlineData(10, false, false, false, false, false, false, true, true, false, LinqMethodTypes.Compiled, true),
         InlineData(10, false, true, false, true, true, true, false, true, false, LinqMethodTypes.Compiled, true),
         InlineData(10, false, true, true, false, true, true, true, true, false, LinqMethodTypes.Compiled, true),
         InlineData(10, true, true, true, false, false, false, true, false, true, LinqMethodTypes.Compiled, true)]
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
            LinqMethodTypes linqMethodTypes, 
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

                        var id = Guid.NewGuid();
                        var producer = new ProducerMethodAsyncShared();
                        await producer.RunTestAsync<PostgreSqlMessageQueueInit>(queueConnection, interceptors, messageCount, logProvider,
                            Helpers.GenerateData,
                            Helpers.Verify, false, 0, id, linqMethodTypes, oCreation.Scope, enableChaos).ConfigureAwait(false);
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
