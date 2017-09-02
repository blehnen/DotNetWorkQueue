// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------

using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.RpcMethod;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using DotNetWorkQueue.Transport.RelationalDatabase;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.RpcMethod
{
    [Collection("PostgreSQL")]
    public class SimpleMethodRpc
    {
        [Theory]
        [InlineData(50, 1, 200, 3, false, false, LinqMethodTypes.Dynamic),
         InlineData(30, 0, 240, 3, false, true, LinqMethodTypes.Compiled)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, bool useTransactions, bool async, LinqMethodTypes linqMethodTypes)
        {
            var queueNameSend = GenerateQueueName.Create();
            var queueNameReceive = GenerateQueueName.Create();
            var logProviderSend = LoggerShared.Create(queueNameSend, GetType().Name);
            var logProviderReceive = LoggerShared.Create(queueNameReceive, GetType().Name);

            using (var queueCreatorReceive =
                new QueueCreationContainer<PostgreSqlMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProviderReceive, LifeStyles.Singleton)))
            {
                using (var queueCreatorSend =
                    new QueueCreationContainer<PostgreSqlMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProviderSend, LifeStyles.Singleton)))
                {
                    try
                    {

                        using (
                            var oCreationReceive =
                                queueCreatorReceive.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueNameReceive,
                                    ConnectionInfo.ConnectionString)
                            )
                        {

                            oCreationReceive.Options.EnableDelayedProcessing = true;
                            oCreationReceive.Options.EnableHeartBeat = !useTransactions;
                            oCreationReceive.Options.EnableHoldTransactionUntilMessageCommitted = useTransactions;
                            oCreationReceive.Options.EnableStatus = !useTransactions;
                            oCreationReceive.Options.EnableStatusTable = true;
                            oCreationReceive.Options.QueueType = QueueTypes.RpcReceive;

                            var resultReceive = oCreationReceive.CreateQueue();
                            Assert.True(resultReceive.Success, resultReceive.ErrorMessage);

                            using (
                                var oCreation =
                                    queueCreatorSend.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueNameSend,
                                        ConnectionInfo.ConnectionString)
                                )
                            {
                                oCreation.Options.EnableDelayedProcessing = true;
                                oCreation.Options.EnableHeartBeat = !useTransactions;
                                oCreation.Options.EnableHoldTransactionUntilMessageCommitted = useTransactions;
                                oCreation.Options.EnableStatus = !useTransactions;
                                oCreation.Options.EnableStatusTable = true;
                                oCreation.Options.QueueType = QueueTypes.RpcSend;

                                var result = oCreation.CreateQueue();
                                Assert.True(result.Success, result.ErrorMessage);
                                var id = Guid.NewGuid();

                                var rpc =
                                    new RpcMethodShared
                                        <PostgreSqlMessageQueueInit, PostgreSqlRpcConnection>();

                                rpc.Run(queueNameReceive, queueNameSend, ConnectionInfo.ConnectionString,
                                    ConnectionInfo.ConnectionString, logProviderReceive, logProviderSend,
                                    runtime, messageCount, workerCount, timeOut, async,
                                    new PostgreSqlRpcConnection(ConnectionInfo.ConnectionString, queueNameSend,
                                        ConnectionInfo.ConnectionString, queueNameReceive), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), id, linqMethodTypes);

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
                                queueCreatorSend.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueNameSend,
                                    ConnectionInfo.ConnectionString)
                            )
                        {
                            oCreation.RemoveQueue();
                        }

                        using (
                            var oCreation =
                                queueCreatorReceive.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueNameReceive,
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
