using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.RpcMethod;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.RpcMethod
{
    [Collection("SqlServer")]
    public class SimpleMethodRpc
    {
        [Theory]
        [InlineData(10, 1, 180, 3, true, LinqMethodTypes.Compiled),
#if NETFULL
        InlineData(50, 1, 200, 3, false, LinqMethodTypes.Dynamic),
        InlineData(10, 1, 180, 3, true, LinqMethodTypes.Dynamic),
#endif
        InlineData(10, 1, 180, 3, false, LinqMethodTypes.Compiled)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, bool async, LinqMethodTypes linqMethodTypes)
        {
            var queueNameSend = GenerateQueueName.Create();
            var queueNameReceive = GenerateQueueName.Create();
            var logProviderSend = LoggerShared.Create(queueNameSend, GetType().Name);
            var logProviderReceive = LoggerShared.Create(queueNameReceive, GetType().Name);

            using (var queueCreatorReceive =
                new QueueCreationContainer<SqlServerMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProviderReceive, LifeStyles.Singleton)))
            {
                using (var queueCreatorSend =
                    new QueueCreationContainer<SqlServerMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProviderSend, LifeStyles.Singleton)))
                {
                    try
                    {

                        using (
                            var oCreationReceive =
                                queueCreatorReceive.GetQueueCreation<SqlServerMessageQueueCreation>(queueNameReceive,
                                    ConnectionInfo.ConnectionString)
                            )
                        {

                            oCreationReceive.Options.EnableDelayedProcessing = true;
                            oCreationReceive.Options.EnableHeartBeat = true;
                            oCreationReceive.Options.EnableHoldTransactionUntilMessageCommitted = false;
                            oCreationReceive.Options.EnableStatus = true;
                            oCreationReceive.Options.EnableStatusTable = true;
                            oCreationReceive.Options.QueueType = QueueTypes.RpcReceive;

                            var resultReceive = oCreationReceive.CreateQueue();
                            Assert.True(resultReceive.Success, resultReceive.ErrorMessage);

                            using (
                                var oCreation =
                                    queueCreatorSend.GetQueueCreation<SqlServerMessageQueueCreation>(queueNameSend,
                                        ConnectionInfo.ConnectionString)
                                )
                            {
                                oCreation.Options.EnableDelayedProcessing = true;
                                oCreation.Options.EnableHeartBeat = true;
                                oCreation.Options.EnableHoldTransactionUntilMessageCommitted = false;
                                oCreation.Options.EnableStatus = true;
                                oCreation.Options.EnableStatusTable = true;
                                oCreation.Options.QueueType = QueueTypes.RpcSend;

                                var result = oCreation.CreateQueue();
                                Assert.True(result.Success, result.ErrorMessage);
                                var id = Guid.NewGuid();

                                var rpc =
                                    new RpcMethodShared
                                        <SqlServerMessageQueueInit, SqlServerRpcConnection>();

                                rpc.Run(queueNameReceive, queueNameSend, ConnectionInfo.ConnectionString,
                                    ConnectionInfo.ConnectionString, logProviderReceive, logProviderSend,
                                    runtime, messageCount, workerCount, timeOut, async,
                                    new SqlServerRpcConnection(ConnectionInfo.ConnectionString, queueNameSend,
                                        ConnectionInfo.ConnectionString, queueNameReceive), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), id, linqMethodTypes, "second(*%3)");

                                new VerifyQueueRecordCount(queueNameSend, oCreation.Options).Verify(0, false, false);
                                new VerifyQueueRecordCount(queueNameReceive, oCreationReceive.Options).Verify(0, false,
                                    false);
                            }
                        }
                    }
                    finally
                    {
                        using (
                            var oCreation =
                                queueCreatorSend.GetQueueCreation<SqlServerMessageQueueCreation>(queueNameSend,
                                    ConnectionInfo.ConnectionString)
                            )
                        {
                            oCreation.RemoveQueue();
                        }

                        using (
                            var oCreation =
                                queueCreatorReceive.GetQueueCreation<SqlServerMessageQueueCreation>(queueNameReceive,
                                    ConnectionInfo.ConnectionString)
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
