using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.ProducerMethod
{
    [Collection("producer")]
    public class SimpleProducerMethodBatch
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
         InlineData(500, false, true, true, false, true, true, true, true, false, LinqMethodTypes.Dynamic, false),
         InlineData(500, true, true, true, false, false, false, true, false, true, LinqMethodTypes.Dynamic, false),
#endif        
         InlineData(100, false, true, true, false, false, false, true, false, false, LinqMethodTypes.Compiled, false),
         InlineData(100, false, false, false, false, false, false, false, false, false, LinqMethodTypes.Compiled, false),
         InlineData(100, true, false, false, false, false, false, false, false, false, LinqMethodTypes.Compiled, false),
         InlineData(100, false, false, false, false, false, false, false, true, false, LinqMethodTypes.Compiled, false),
         InlineData(100, false, false, false, false, false, false, true, true, false, LinqMethodTypes.Compiled, false),
         InlineData(100, false, true, false, true, true, true, false, true, false, LinqMethodTypes.Compiled, false),
         InlineData(500, false, true, true, false, true, true, true, true, false, LinqMethodTypes.Compiled, false),
         InlineData(500, true, true, true, false, false, false, true, false, true, LinqMethodTypes.Compiled, false),

         InlineData(10, false, true, true, false, false, false, true, false, false, LinqMethodTypes.Compiled, true),
         InlineData(10, false, false, false, false, false, false, false, false, false, LinqMethodTypes.Compiled, true),
         InlineData(10, true, false, false, false, false, false, false, false, false, LinqMethodTypes.Compiled, true),
         InlineData(10, false, false, false, false, false, false, false, true, false, LinqMethodTypes.Compiled, true),
         InlineData(10, false, false, false, false, false, false, true, true, false, LinqMethodTypes.Compiled, true),
         InlineData(10, false, true, false, true, true, true, false, true, false, LinqMethodTypes.Compiled, true),
         InlineData(50, false, true, true, false, true, true, true, true, false, LinqMethodTypes.Compiled, true),
         InlineData(50, true, true, true, false, false, false, true, false, true, LinqMethodTypes.Compiled, true)]
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
            LinqMethodTypes linqMethodTypes,
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
                        oCreation.Options.EnableHoldTransactionUntilMessageCommitted = enableHoldTransactionUntilMessageCommitted;
                        oCreation.Options.EnablePriority = enablePriority;
                        oCreation.Options.EnableStatus = enableStatus;
                        oCreation.Options.EnableStatusTable = enableStatusTable;

                        if (additionalColumn)
                        {
                            oCreation.Options.AdditionalColumns.Add(new Column("OrderID", ColumnTypes.Integer, false));
                        }

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);

                        var producer = new ProducerMethodShared();
                        if (linqMethodTypes == LinqMethodTypes.Compiled)
                        {
                            producer.RunTestCompiled<PostgreSqlMessageQueueInit>(queueName,
                           ConnectionInfo.ConnectionString, interceptors, messageCount, logProvider,
                           Helpers.GenerateData,
                           Helpers.Verify, true, Guid.NewGuid(), GenerateMethod.CreateCompiled, 0, oCreation.Scope, enableChaos);
                        }
#if NETFULL
                        else
                        {
                            producer.RunTestDynamic<PostgreSqlMessageQueueInit>(queueName,
                           ConnectionInfo.ConnectionString, interceptors, messageCount, logProvider,
                           Helpers.GenerateData,
                           Helpers.Verify, true, Guid.NewGuid(), GenerateMethod.CreateDynamic, 0, oCreation.Scope, enableChaos);
                        }
#endif
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
