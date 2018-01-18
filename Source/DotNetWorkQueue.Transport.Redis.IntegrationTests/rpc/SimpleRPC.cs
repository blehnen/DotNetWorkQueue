using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Rpc;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.rpc
{
    [Collection("Redis")]
    public class SimpleRpc
    {
        [Theory]
        [InlineData(10, 1, 180, 5, false, ConnectionInfoTypes.Linux),
         InlineData(30, 0, 240, 5, true, ConnectionInfoTypes.Linux),
         InlineData(30, 0, 240, 5, false, ConnectionInfoTypes.Windows),
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
                        new RedisQueueRpcConnection(connectionString, queueNameSend), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)");

                    using (var count = new VerifyQueueRecordCount(queueNameSend, connectionString))
                    {
                        count.Verify(0, false, -1);
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
