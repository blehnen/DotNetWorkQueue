using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.ConsumerMethod
{
    [Collection("SQLite")]
    public class ConsumerMethodExpiredMessage
    {
        [Theory]
        [InlineData(100, 0, 20, 5, true, LinqMethodTypes.Compiled),
         InlineData(100, 0, 20, 5, false, LinqMethodTypes.Compiled),
          InlineData(100, 0, 20, 5, true, LinqMethodTypes.Dynamic),
         InlineData(100, 0, 20, 5, false, LinqMethodTypes.Dynamic)]
        public void Run(int messageCount, int runtime, 
            int timeOut, int workerCount, bool inMemoryDb, LinqMethodTypes linqMethodTypes)
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
                    try
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
                            )
                        {
                            oCreation.Options.EnableMessageExpiration = true;
                            oCreation.Options.EnableDelayedProcessing = true;
                            oCreation.Options.EnableHeartBeat = true;
                            oCreation.Options.EnableStatus = true;
                            oCreation.Options.EnableStatusTable = true;

                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            var producer = new ProducerMethodShared();
                            var id = Guid.NewGuid();

                            if (linqMethodTypes == LinqMethodTypes.Compiled)
                            {
                                producer.RunTestCompiled<SqLiteMessageQueueInit>(queueName,
                               connectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                               Helpers.Verify, false, id, GenerateMethod.CreateNoOpCompiled, runtime, oCreation.Scope);
                            }
                            else
                            {
                                producer.RunTestDynamic<SqLiteMessageQueueInit>(queueName,
                               connectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                               Helpers.Verify, false, id, GenerateMethod.CreateNoOpDynamic, runtime, oCreation.Scope);
                            }

                            var consumer = new ConsumerMethodExpiredMessageShared();
                            consumer.RunConsumer<SqLiteMessageQueueInit>(queueName, connectionInfo.ConnectionString,
                                false,
                                logProvider,
                                runtime, messageCount,
                                workerCount, timeOut, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)", id);

                            new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options).Verify(0, false, false);
                        }
                    }
                    finally
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
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
