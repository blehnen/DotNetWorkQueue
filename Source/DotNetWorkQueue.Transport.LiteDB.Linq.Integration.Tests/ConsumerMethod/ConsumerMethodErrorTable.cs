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
    public class ConsumerMethodErrorTable
    {
        [Theory]
        [InlineData(10, 60, 5,  LinqMethodTypes.Dynamic, false),
         InlineData(1, 60, 1, LinqMethodTypes.Compiled, true)]
        public void Run(int messageCount, int timeOut, int workerCount, LinqMethodTypes linqMethodTypes, bool enableChaos)
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
                               Helpers.Verify, false, id, GenerateMethod.CreateErrorCompiled, 0, oCreation.Scope, false);
                            }
                            else
                            {
                                producer.RunTestDynamic<LiteDbMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                                  Helpers.Verify, false, id, GenerateMethod.CreateErrorDynamic, 0, oCreation.Scope, false);

                            }

                            //process data
                            var consumer = new ConsumerMethodErrorShared();
                            consumer.RunConsumer<LiteDbMessageQueueInit>(queueConnection,
                                false,
                                logProvider,
                                workerCount, timeOut, messageCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id, "second(*%10)", enableChaos);
                            ValidateErrorCounts(queueName, connectionInfo.ConnectionString, messageCount);
                            new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options).Verify(messageCount, true, false);

                            consumer.PurgeErrorMessages<LiteDbMessageQueueInit>(queueConnection,
                                false, logProvider, false);
                            ValidateErrorCounts(queueName, connectionInfo.ConnectionString, messageCount);

                            //purge error messages and verify that count is 0
                            consumer.PurgeErrorMessages<LiteDbMessageQueueInit>(queueConnection,
                                false,  logProvider, true);
                            ValidateErrorCounts(queueName, connectionInfo.ConnectionString, 0);
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

        private void ValidateErrorCounts(string queueName, string connectionString, int messageCount)
        {
            new VerifyErrorCounts(queueName, connectionString).Verify(messageCount, 2);
        }
    }
}
