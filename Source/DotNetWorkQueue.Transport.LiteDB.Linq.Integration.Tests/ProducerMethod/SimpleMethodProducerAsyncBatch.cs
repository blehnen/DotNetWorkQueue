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
        [InlineData(1000, true, true, true, false,  LinqMethodTypes.Dynamic, false),
         InlineData(100, false, true, true, false,  LinqMethodTypes.Dynamic, false),
         InlineData(100, false, false, false, false,  LinqMethodTypes.Dynamic, false),
         InlineData(100, true, false, false, false, LinqMethodTypes.Dynamic, false),
         InlineData(100, true, true, true, false, LinqMethodTypes.Compiled, false),
         InlineData(100, false, true, true, false,  LinqMethodTypes.Compiled, false),
         InlineData(100, false, false, false, false,  LinqMethodTypes.Compiled, false),
         InlineData(1000, true, false, false, false,  LinqMethodTypes.Compiled, false),

         InlineData(100, true, true, true, false,  LinqMethodTypes.Dynamic, true),
         InlineData(10, false, true, true, false,  LinqMethodTypes.Dynamic, true),
         InlineData(10, false, false, false, false,  LinqMethodTypes.Dynamic, true),
         InlineData(10, true, false, false, false,  LinqMethodTypes.Dynamic, true),
         InlineData(10, true, true, true, false,  LinqMethodTypes.Compiled, true),
         InlineData(10, false, true, true, false, LinqMethodTypes.Compiled, true),
         InlineData(10, false, false, false, false, LinqMethodTypes.Compiled, true),
         InlineData(100, true, false, false, false,  LinqMethodTypes.Compiled, true)]
        public async void Run(
            int messageCount,
            bool interceptors,
            bool enableDelayedProcessing,
            bool enableMessageExpiration,
            bool enableStatusTable,
            LinqMethodTypes linqMethodTypes,
            bool enableChaos)
        {

            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (
                    var queueCreator =
                        new QueueCreationContainer<LiteDbMessageQueueInit>(
                            serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, connectionInfo.ConnectionString);
                    try
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection)
                            )
                        {
                            oCreation.Options.EnableDelayedProcessing = enableDelayedProcessing;
                            oCreation.Options.EnableMessageExpiration = enableMessageExpiration;
                            oCreation.Options.EnableStatusTable = enableStatusTable;

                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            var producer = new ProducerMethodAsyncShared();
                            var id = Guid.NewGuid();
                            await producer.RunTestAsync<LiteDbMessageQueueInit>(queueConnection, interceptors, messageCount, logProvider,
                                Helpers.GenerateData,
                                Helpers.Verify, true, 0, id, linqMethodTypes, oCreation.Scope, enableChaos).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection)
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
