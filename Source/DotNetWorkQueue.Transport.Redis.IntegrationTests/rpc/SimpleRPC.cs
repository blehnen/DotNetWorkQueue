// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.rpc
{
    [Collection("RedisProducerTests")]
    public class SimpleRpc
    {
        [Theory]
        [InlineData(10, 1, 180, 5, false),
         InlineData(30, 0, 240, 5, false),
         InlineData(10, 1, 180, 5, true),
         InlineData(30, 0, 240, 5, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, bool async)
        {
            var queueNameSend = GenerateQueueName.Create();
            var logProviderSend = LoggerShared.Create(queueNameSend);
            using (var queueCreatorSend =
                new QueueCreationContainer<RedisQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProviderSend, LifeStyles.Singleton)))
            {
                try
                {
                    var rpc =
                        new RpcShared
                            <RedisQueueInit, FakeResponse, FakeMessage, RedisQueueRpcConnection>();
                    rpc.Run(queueNameSend, queueNameSend, ConnectionInfo.ConnectionString,
                        ConnectionInfo.ConnectionString, logProviderSend, logProviderSend,
                        runtime, messageCount, workerCount, timeOut, async,
                        new RedisQueueRpcConnection(ConnectionInfo.ConnectionString, queueNameSend));

                    new VerifyQueueRecordCount(queueNameSend).Verify(0, false);

                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreatorSend.GetQueueCreation<RedisQueueCreation>(queueNameSend,
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
