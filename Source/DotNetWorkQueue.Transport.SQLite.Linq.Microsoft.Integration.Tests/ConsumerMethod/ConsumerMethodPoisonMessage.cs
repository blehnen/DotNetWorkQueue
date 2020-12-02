using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using DotNetWorkQueue.Transport.SQLite.Microsoft.Basic;
using DotNetWorkQueue.Transport.SQLite.Microsoft.Integration.Tests;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Microsoft.Integration.Tests.ConsumerMethod
{
    [Collection("Consumer")]
    public class ConsumerMethodPoisonMessage
    {
        [Theory]
        [InlineData(1, 60, 1, true, LinqMethodTypes.Compiled, true),
         InlineData(10, 60, 5, true, LinqMethodTypes.Compiled, false)]
        public void Run(int messageCount, int timeOut, 
            int workerCount, bool inMemoryDb, LinqMethodTypes linqMethodTypes, bool enableChaos)
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
                               Helpers.Verify, false, id, GenerateMethod.CreateNoOpCompiled, 0, oCreation.Scope, false);
                            }

                            //process data
                            var consumer = new ConsumerMethodPoisonMessageShared();

                            consumer.RunConsumer<SqLiteMessageQueueInit>(queueConnection,
                                false,
                                workerCount,
                                logProvider, timeOut, messageCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", enableChaos);

                            ValidateErrorCounts(queueConnection, messageCount);
                            new VerifyQueueRecordCount(queueConnection, oCreation.Options).Verify(messageCount, true, true);
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

        private void ValidateErrorCounts(QueueConnection queueConnection, long messageCount)
        {
            //poison messages are moved to the error queue right away
            //they don't update the tracking table, so specify 0 for the error count.
            //They still update the error table itself
            new VerifyErrorCounts(queueConnection).Verify(messageCount, 0);
        }
    }
}
