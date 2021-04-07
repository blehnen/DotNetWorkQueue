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
    public class ConsumerMethodPoisonMessage
    {
        [Theory]
        [InlineData(1, 60, 1, LinqMethodTypes.Dynamic, false),
         InlineData(10, 60, 5, LinqMethodTypes.Compiled, false)]
        public void Run(int messageCount, int timeOut, 
            int workerCount, LinqMethodTypes linqMethodTypes, bool enableChaos)
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
                               Helpers.Verify, false, id, GenerateMethod.CreateNoOpCompiled, 0, oCreation.Scope, false);
                            }
                            else
                            {
                                producer.RunTestDynamic<LiteDbMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                               Helpers.Verify, false, id, GenerateMethod.CreateNoOpDynamic, 0, oCreation.Scope, false);
                            }

                            //process data
                            var consumer = new ConsumerMethodPoisonMessageShared();

                            consumer.RunConsumer<LiteDbMessageQueueInit>(queueConnection,
                                false,
                                workerCount,
                                logProvider, timeOut, messageCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", enableChaos);

                            ValidateErrorCounts(queueName, connectionInfo.ConnectionString, messageCount);
                            new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options).Verify(messageCount, true, true);
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

        private void ValidateErrorCounts(string queueName, string connectionString, long messageCount)
        {
            //poison messages are moved to the error queue right away
            //they don't update the tracking table, so specify 0 for the error count.
            //They still update the error table itself
            new VerifyErrorCounts(queueName, connectionString).Verify(messageCount, 0);
        }
    }
}
