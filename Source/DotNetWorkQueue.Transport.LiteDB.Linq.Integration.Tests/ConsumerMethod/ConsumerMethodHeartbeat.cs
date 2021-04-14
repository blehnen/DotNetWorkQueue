using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.ConsumerMethod
{
    [Collection("Consumer")]
    public class ConsumerMethodHeartbeat
    {
        [Theory]
        [InlineData(7, 15, 190, 3, LinqMethodTypes.Dynamic, true, IntegrationConnectionInfo.ConnectionTypes.Direct),
        InlineData(7, 15, 90, 3,  LinqMethodTypes.Compiled, false, IntegrationConnectionInfo.ConnectionTypes.Memory)]
        public void Run(int messageCount, int runtime, 
            int timeOut, int workerCount, LinqMethodTypes linqMethodTypes, bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
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

                        oCreation.Options.EnableDelayedProcessing = true;
                        oCreation.Options.EnableStatusTable = true;

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);
                        scope = oCreation.Scope;

                        var producer = new ProducerMethodShared();
                        var id = Guid.NewGuid();
                        if (linqMethodTypes == LinqMethodTypes.Compiled)
                        {
                            producer.RunTestCompiled<LiteDbMessageQueueInit>(queueConnection, false, messageCount,
                                logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, id, GenerateMethod.CreateCancelCompiled, runtime,
                                oCreation.Scope, false);
                        }
                        else
                        {
                            producer.RunTestDynamic<LiteDbMessageQueueInit>(queueConnection, false, messageCount,
                                logProvider, Helpers.GenerateData,
                                Helpers.Verify, false, id, GenerateMethod.CreateCancelDynamic, runtime, oCreation.Scope,
                                false);
                        }


                        var consumer = new ConsumerMethodHeartBeatShared();
                        consumer.RunConsumer<LiteDbMessageQueueInit>(queueConnection,
                            false,
                            logProvider,
                            runtime, messageCount,
                            workerCount, timeOut, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), id, "second(*%3)",
                            enableChaos, scope);

                        new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options, scope)
                            .Verify(0, false, false);
                        GenerateMethod.ClearCancel(id);

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
