using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.ConsumerMethodAsync
{
    [Collection("ConsumerAsync")]
    public class ConsumerMethodAsyncRollBack
    {
        [Theory]
        [InlineData(100, 1, 400, 5, 5, 5, LinqMethodTypes.Dynamic, false),
         InlineData(50, 5, 200, 5, 1, 3, LinqMethodTypes.Compiled, false)]
        public void Run(int messageCount, int runtime, int timeOut, 
            int workerCount, int readerCount, int queueSize, LinqMethodTypes linqMethodTypes, bool enableChaos)
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
                            oCreation.Options.EnableDelayedProcessing = true;
                            oCreation.Options.EnableStatusTable = true;

                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            //create data
                            var producer = new ProducerMethodShared();
                            var id = Guid.NewGuid();
                            if (linqMethodTypes == LinqMethodTypes.Compiled)
                            {
                                producer.RunTestCompiled<LiteDbMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                   Helpers.Verify, false, id, GenerateMethod.CreateRollBackCompiled, runtime, null, false);
                            }
                            else
                            {
                                producer.RunTestDynamic<LiteDbMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                   Helpers.Verify, false, id, GenerateMethod.CreateRollBackDynamic, runtime, null, false);
                            }

                            //process data
                            var consumer = new ConsumerMethodAsyncRollBackShared();
                            consumer.RunConsumer<LiteDbMessageQueueInit>(queueConnection,
                                false,
                                workerCount, logProvider,
                                timeOut, readerCount, queueSize, runtime, messageCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id, "second(*%10)", enableChaos);
                            LoggerShared.CheckForErrors(queueName);
                            new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options).Verify(0, false, false);
                            GenerateMethod.ClearRollback(id);
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
