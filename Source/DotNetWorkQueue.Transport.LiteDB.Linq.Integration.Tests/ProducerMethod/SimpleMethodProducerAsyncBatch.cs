using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.ProducerMethod
{
    [Collection("Producer")]
    public class SimpleMethodProducerAsyncBatch
    {
        [Theory]
        [InlineData(1000, true, true, true, false,  LinqMethodTypes.Dynamic, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, false, true, true, false,  LinqMethodTypes.Dynamic, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, false, false, false, false,  LinqMethodTypes.Dynamic, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, true, false, false, false, LinqMethodTypes.Dynamic, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, true, true, true, false, LinqMethodTypes.Compiled, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, false, true, true, false,  LinqMethodTypes.Compiled, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(100, false, false, false, false,  LinqMethodTypes.Compiled, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(1000, true, false, false, false,  LinqMethodTypes.Compiled, false, IntegrationConnectionInfo.ConnectionTypes.Direct),

         InlineData(100, true, true, true, false,  LinqMethodTypes.Dynamic, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, false, true, true, false,  LinqMethodTypes.Dynamic, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, false, false, false, false,  LinqMethodTypes.Dynamic, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, true, false, false, false,  LinqMethodTypes.Dynamic, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, true, true, true, false,  LinqMethodTypes.Compiled, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, false, true, true, false, LinqMethodTypes.Compiled, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(10, false, false, false, false, LinqMethodTypes.Compiled, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
         InlineData(100, true, false, false, false,  LinqMethodTypes.Compiled, true, IntegrationConnectionInfo.ConnectionTypes.Shared)]
        public async void Run(
            int messageCount,
            bool interceptors,
            bool enableDelayedProcessing,
            bool enableMessageExpiration,
            bool enableStatusTable,
            LinqMethodTypes linqMethodTypes,
            bool enableChaos,
            IntegrationConnectionInfo.ConnectionTypes connectionType)
        {

            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (
                    var queueCreator =
                        new QueueCreationContainer<LiteDbMessageQueueInit>(
                            serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, connectionInfo.ConnectionString);
                    ICreationScope scope = null;
                    var oCreation = queueCreator.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection);
                    try
                    {

                      
                            oCreation.Options.EnableDelayedProcessing = enableDelayedProcessing;
                            oCreation.Options.EnableMessageExpiration = enableMessageExpiration;
                            oCreation.Options.EnableStatusTable = enableStatusTable;

                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);
                            scope = oCreation.Scope;

                            var producer = new ProducerMethodAsyncShared();
                            var id = Guid.NewGuid();
                            await producer.RunTestAsync<LiteDbMessageQueueInit>(queueConnection, interceptors, messageCount, logProvider,
                                Helpers.GenerateData,
                                Helpers.Verify, true, 0, id, linqMethodTypes, oCreation.Scope, enableChaos).ConfigureAwait(false);
                        
                    }
                    finally
                    {
                       
                            oCreation.RemoveQueue();
                        oCreation.Dispose();
                        scope?.Dispose();
                    }
                }
            }
        }
    }
}
