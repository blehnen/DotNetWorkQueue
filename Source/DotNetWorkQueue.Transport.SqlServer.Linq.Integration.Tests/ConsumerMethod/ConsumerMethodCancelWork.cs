using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.ConsumerMethod
{
    [Collection("Consumer")]
    public class ConsumerMethodCancelWork
    {
        [Theory]
#if NETFULL
        [InlineData(7, 15, 90, 3, LinqMethodTypes.Compiled, false),
        InlineData(7, 15, 90, 3, LinqMethodTypes.Dynamic, false)]
#else
        [InlineData(7, 15, 190, 3, LinqMethodTypes.Compiled, true)]
#endif
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (
                var queueCreator =
                    new QueueCreationContainer<SqlServerMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, ConnectionInfo.ConnectionString);
                try
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection)
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
                            producer.RunTestCompiled<SqlServerMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                          Helpers.Verify, false, id, GenerateMethod.CreateCancelCompiled, runtime, oCreation.Scope, false);
                        }
#if NETFULL
                        else
                        {
                            producer.RunTestDynamic<SqlServerMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                          Helpers.Verify, false, id, GenerateMethod.CreateCancelDynamic, runtime, oCreation.Scope, false);
                        }
#endif
                        var consumer = new ConsumerMethodCancelWorkShared<SqlServerMessageQueueInit>();
                        consumer.RunConsumer(queueConnection, false, logProvider,
                            runtime, messageCount,
                            workerCount, timeOut, serviceRegister => serviceRegister.Register<IMessageMethodHandling>(() => new MethodMessageProcessingCancel(id), LifeStyles.Singleton), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)", id, enableChaos, new CreationScopeNoOp());

                        new VerifyQueueRecordCount(queueConnection, oCreation.Options).Verify(0, false, false);
                        GenerateMethod.ClearCancel(id);
                    }
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }
    }
}
