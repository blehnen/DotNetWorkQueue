using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.ProducerMethod
{
    [Collection("Producer")]
    public class SimpleMethodProducerBatch
    {
        [Theory]
        [InlineData(1000, true, true, true, false,  LinqMethodTypes.Dynamic, false),
         InlineData(100, false, true, true, false,  LinqMethodTypes.Dynamic, false),
         InlineData(100, false, false, false, false, LinqMethodTypes.Dynamic, false),
         InlineData(100, true, false, false, false, LinqMethodTypes.Dynamic, false),
         InlineData(100, true, true, true, false, LinqMethodTypes.Compiled, false),
         InlineData(100, false, true, true, false, LinqMethodTypes.Compiled, false),
         InlineData(100, false, false, false, false,  LinqMethodTypes.Compiled, false),
         InlineData(1000, true, false, false, false, LinqMethodTypes.Compiled, false),

         InlineData(100, true, true, true, false,  LinqMethodTypes.Dynamic, true),
         InlineData(10, false, true, true, false,  LinqMethodTypes.Dynamic, true),
         InlineData(10, false, false, false, false, LinqMethodTypes.Dynamic, true),
         InlineData(10, true, false, false, false,  LinqMethodTypes.Dynamic, true),
         InlineData(10, true, true, true, false,  LinqMethodTypes.Compiled, true),
         InlineData(10, false, true, true, false,  LinqMethodTypes.Compiled, true),
         InlineData(10, false, false, false, false,  LinqMethodTypes.Compiled, true),
         InlineData(100, true, false, false, false, LinqMethodTypes.Compiled, true)]
        public void Run(
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
                using (var queueCreator =
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

                            var producer = new ProducerMethodShared();
                            var id = Guid.NewGuid();
                            if (linqMethodTypes == LinqMethodTypes.Compiled)
                            {
                                producer.RunTestCompiled<LiteDbMessageQueueInit>(queueConnection, interceptors, messageCount, logProvider,
                               Helpers.GenerateData,
                               Helpers.Verify, true, id, GenerateMethod.CreateCompiled, 0, oCreation.Scope, enableChaos);
                            }
                            else
                            {
                                producer.RunTestDynamic<LiteDbMessageQueueInit>(queueConnection, interceptors, messageCount, logProvider,
                               Helpers.GenerateData,
                               Helpers.Verify, true, id, GenerateMethod.CreateDynamic, 0, oCreation.Scope, enableChaos);
                            }
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
