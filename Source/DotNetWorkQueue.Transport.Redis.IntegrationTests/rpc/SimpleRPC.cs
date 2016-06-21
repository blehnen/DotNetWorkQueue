// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Rpc;
using DotNetWorkQueue.Transport.Redis.Basic;
using System;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.rpc
{
    [Collection("Redis")]
    public class SimpleRpc
    {
        [Theory]
        [InlineData(10, 1, 180, 5, false, ConnectionInfoTypes.Linux),
         InlineData(30, 0, 240, 5, false, ConnectionInfoTypes.Linux),
         InlineData(10, 1, 180, 5, true, ConnectionInfoTypes.Linux),
         InlineData(30, 0, 240, 5, true, ConnectionInfoTypes.Linux),
            InlineData(10, 1, 180, 5, false, ConnectionInfoTypes.Windows),
         InlineData(30, 0, 240, 5, false, ConnectionInfoTypes.Windows),
         InlineData(10, 1, 180, 5, true, ConnectionInfoTypes.Windows),
         InlineData(30, 0, 240, 5, true, ConnectionInfoTypes.Windows)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, bool async, ConnectionInfoTypes type)
        {
            var queueNameSend = GenerateQueueName.Create();
            var logProviderSend = LoggerShared.Create(queueNameSend, GetType().Name);
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (var queueCreatorSend =
                new QueueCreationContainer<RedisQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProviderSend, LifeStyles.Singleton)))
            {
                try
                {
                    var rpc =
                        new RpcShared
                            <RedisQueueInit, FakeResponse, FakeMessage, RedisQueueRpcConnection>();
                    rpc.Run(queueNameSend, queueNameSend, connectionString,
                        connectionString, logProviderSend, logProviderSend,
                        runtime, messageCount, workerCount, timeOut, async,
                        new RedisQueueRpcConnection(connectionString, queueNameSend), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12));

                    using (var count = new VerifyQueueRecordCount(queueNameSend, connectionString))
                    {
                        count.Verify(0, false);
                    }
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreatorSend.GetQueueCreation<RedisQueueCreation>(queueNameSend,
                                connectionString)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }
    }
}
