using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.Consumer
{
    [Collection("consumer")]
    public class SimpleConsumer
    {
        [Theory]
        [InlineData(1000, 0, 240, 5),
        InlineData(50, 5, 200, 10),
        InlineData(10, 15, 180, 7)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var producer = new DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.SimpleConsumer();
                producer.Run<MemoryMessageQueueInit, FakeMessage, MessageQueueCreation>(queueName,
                    connectionInfo.ConnectionString,
                    messageCount, runtime, timeOut, workerCount, false, x => { },
                    Helpers.GenerateData, Helpers.Verify, VerifyQueueCount);
            }
        }

        private void VerifyQueueCount(string arg1, string arg2, IBaseTransportOptions arg3, ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            new VerifyQueueRecordCount().Verify(arg4, arg5, true);
        }
    }
}
