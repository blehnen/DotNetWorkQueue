using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using DotNetWorkQueue.Transport.SQLite.Microsoft.Basic;
using DotNetWorkQueue.Transport.SQLite.Microsoft.Integration.Tests;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Microsoft.Integration.Tests.ConsumerMethodAsync
{
    [Collection("ConsumerAsync")]
    public class ConsumerMethodAsyncRollBack
    {
        [Theory]
        [InlineData(100, 1, 400, 5, 5, 5, false, LinqMethodTypes.Compiled, false),
         InlineData(50, 5, 200, 5, 1, 3, true, LinqMethodTypes.Compiled, true)]
        public void Run(int messageCount, int runtime, int timeOut, 
            int workerCount, int readerCount, int queueSize, bool inMemoryDb, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
                    new QueueCreationContainer<SqLiteMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, connectionInfo.ConnectionString);
                    try
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection)
                            )
                        {
                            oCreation.Options.EnableDelayedProcessing = true;
                            oCreation.Options.EnableHeartBeat = true;
                            oCreation.Options.EnableStatus = true;
                            oCreation.Options.EnableStatusTable = true;

                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            //create data
                            var producer = new ProducerMethodShared();
                            var id = Guid.NewGuid();
                            if (linqMethodTypes == LinqMethodTypes.Compiled)
                            {
                                producer.RunTestCompiled<SqLiteMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                   Helpers.Verify, false, id, GenerateMethod.CreateRollBackCompiled, runtime, null, false);
                            }

                            //process data
                            var consumer = new ConsumerMethodAsyncRollBackShared();
                            consumer.RunConsumer<SqLiteMessageQueueInit>(queueConnection,
                                false,
                                workerCount, logProvider,
                                timeOut, readerCount, queueSize, runtime, messageCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id, "second(*%10)", enableChaos);
                            LoggerShared.CheckForErrors(queueName);
                            new VerifyQueueRecordCount(queueConnection, oCreation.Options).Verify(0, false, false);
                            GenerateMethod.ClearRollback(id);
                        }
                    }
                    finally
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection)
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
