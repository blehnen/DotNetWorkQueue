using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using NSubstitute.Extensions;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.ConsumerMethodAsync
{
    [Collection("ConsumerAsync")]
    public class ConsumerMethodAsyncErrorTable
    {
        [Theory]
#if NETFULL
        [InlineData(1, 60, 1, 1, 0, false, LinqMethodTypes.Dynamic, false),
        InlineData(1, 60, 1, 1, 0, false, LinqMethodTypes.Compiled, true)]
#else
        [InlineData(1, 60, 1, 1, 0, false, LinqMethodTypes.Compiled, false)]
#endif
        public void Run(int messageCount, int timeOut, int workerCount, 
            int readerCount, int queueSize, bool useTransactions, LinqMethodTypes linqMethodTypes, bool enableChaos)
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
                        oCreation.Options.EnableHeartBeat = !useTransactions;
                        oCreation.Options.EnableHoldTransactionUntilMessageCommitted = useTransactions;
                        oCreation.Options.EnableStatus = !useTransactions;
                        oCreation.Options.EnableStatusTable = true;

                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);

                        var id = Guid.NewGuid();
                        //create data
                        var producer = new ProducerMethodShared();
                        if (linqMethodTypes == LinqMethodTypes.Compiled)
                        {
                            producer.RunTestCompiled<SqlServerMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                          Helpers.Verify, false, id, GenerateMethod.CreateErrorCompiled, 0, oCreation.Scope, false);
                        }
#if NETFULL
                        else
                        {
                            producer.RunTestDynamic<SqlServerMessageQueueInit>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                          Helpers.Verify, false, id, GenerateMethod.CreateErrorDynamic, 0, oCreation.Scope, false);
                        }
#endif
                        //process data
                        var consumer = new ConsumerMethodAsyncErrorShared();
                        consumer.RunConsumer<SqlServerMessageQueueInit>(queueConnection,
                            false,
                            logProvider,
                            messageCount, workerCount, timeOut, queueSize, readerCount,
                            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), id, "second(*%3)", enableChaos, new CreationScopeNoOp());
                        ValidateErrorCounts(queueConnection, messageCount);
                        new VerifyQueueRecordCount(queueConnection, oCreation.Options).Verify(messageCount, true, false);

                        //don't purge
                        consumer.PurgeErrorMessages<SqlServerMessageQueueInit>(queueConnection,
                            false, logProvider, false, new CreationScopeNoOp());
                        ValidateErrorCounts(queueConnection, messageCount);

                        //purge error messages and verify that count is 0
                        consumer.PurgeErrorMessages<SqlServerMessageQueueInit>(queueConnection,
                            false,  logProvider, true, new CreationScopeNoOp());
                        ValidateErrorCounts(queueConnection, 0);
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
        private void ValidateErrorCounts(QueueConnection queueConnection, int messageCount)
        {
            new VerifyErrorCounts(queueConnection).Verify(messageCount, 2);
        }
    }
}
