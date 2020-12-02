using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.ConsumerMethod
{
    [Collection("consumer")]
    public class ConsumerMethodCancelWork
    {
        [Theory]
#if NETFULL
         [InlineData(7, 60, 180, 3, LinqMethodTypes.Compiled, false),
            InlineData(7, 60, 180, 3, LinqMethodTypes.Dynamic, false)]
#else
        [InlineData(7, 60, 90, 3, LinqMethodTypes.Compiled, false)]
#endif
        public void Run(int messageCount, int runtime, int timeOut, 
            int workerCount, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (
                var queueCreator =
                    new QueueCreationContainer<PostgreSqlMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);
                try
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection)
                        )
                    {
                        oCreation.Options.EnableDelayedProcessing = true;
                        oCreation.Options.EnableHeartBeat = true;
                        oCreation.Options.EnableHoldTransactionUntilMessageCommitted = false;
                        oCreation.Options.EnableStatus = true;
                        oCreation.Options.EnableStatusTable = true;

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);

                        var id = Guid.NewGuid();
                        var producer = new ProducerMethodShared();

                        if (linqMethodTypes == LinqMethodTypes.Compiled)
                        {
                            producer.RunTestCompiled<PostgreSqlMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                          Helpers.Verify, false, id, GenerateMethod.CreateCancelCompiled, runtime, oCreation.Scope, false);
                        }
#if NETFULL
                        else
                        {
                            producer.RunTestDynamic<PostgreSqlMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                          Helpers.Verify, false, id, GenerateMethod.CreateCancelDynamic, runtime, oCreation.Scope, false);
                        }
#endif
                        var consumer = new ConsumerMethodCancelWorkShared<PostgreSqlMessageQueueInit>();
                        consumer.RunConsumer(queueConnection, false, logProvider,
                            runtime, messageCount,
                            workerCount, timeOut, serviceRegister => serviceRegister.Register<IMessageMethodHandling>(() => new MethodMessageProcessingCancel(id), LifeStyles.Singleton), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)", id, enableChaos);

                        new VerifyQueueRecordCount(queueName, oCreation.Options).Verify(0, false, false);
                        GenerateMethod.ClearCancel(id);
                    }
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }
    }
}
