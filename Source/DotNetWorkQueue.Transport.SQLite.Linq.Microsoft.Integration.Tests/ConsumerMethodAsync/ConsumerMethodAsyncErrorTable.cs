using System;
using DotNetWorkQueue.Configuration;
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
    public class ConsumerMethodAsyncErrorTable
    {
        [Theory]
        [InlineData(1, 60, 1, 1, 0, false, LinqMethodTypes.Compiled, false),
        InlineData(25, 200, 20, 1, 5, true, LinqMethodTypes.Compiled, true)]
        public void Run(int messageCount, int timeOut, int workerCount, 
            int readerCount, int queueSize, bool inMemoryDb, LinqMethodTypes linqMethodTypes, bool enableChaos)
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
                                producer.RunTestCompiled<SqLiteMessageQueueInit>(queueConnection, false, messageCount, logProvider,
                                    Helpers.GenerateData,
                                    Helpers.Verify, false, id, GenerateMethod.CreateErrorCompiled, 0, oCreation.Scope, false);
                            }

                            //process data
                            var consumer = new ConsumerMethodAsyncErrorShared();
                            consumer.RunConsumer<SqLiteMessageQueueInit>(queueConnection,
                                false,
                                logProvider,
                                messageCount, workerCount, timeOut, queueSize, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id, "second(*%10)", enableChaos);
                            ValidateErrorCounts(queueConnection, messageCount);
                            new VerifyQueueRecordCount(queueConnection, oCreation.Options).Verify(messageCount, true, false);

                            consumer.PurgeErrorMessages<SqLiteMessageQueueInit>(queueConnection,
                                false, logProvider, false);
                            ValidateErrorCounts(queueConnection, messageCount);

                            //purge error messages and verify that count is 0
                            consumer.PurgeErrorMessages<SqLiteMessageQueueInit>(queueConnection,
                                false, logProvider, true);
                            ValidateErrorCounts(queueConnection, 0);
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
        private void ValidateErrorCounts(QueueConnection queueConnection, int messageCount)
        {
            new VerifyErrorCounts(queueConnection).Verify(messageCount, 2);
        }
    }
}
