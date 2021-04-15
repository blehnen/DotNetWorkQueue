using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.ConsumerAsync
{
    [Collection("consumerasync")]
    public class ConsumerAsyncErrorTable
    {
        [Theory]
        [InlineData(1, 20, 1, 1, 0),
        InlineData(25, 30, 20, 1, 5)]
        public void Run(int messageCount, int timeOut, int workerCount, int readerCount, int queueSize)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.ConsumerAsyncErrorTable();
                consumer.Run<MemoryMessageQueueInit, FakeMessage, MessageQueueCreation>(queueName,
                    connectionInfo.ConnectionString,
                    messageCount, timeOut, workerCount,  readerCount, queueSize, false, x => { },
                    Helpers.GenerateData, Helpers.Verify, VerifyQueueCount, ValidateErrorCounts);
            }
        }

        private void ValidateErrorCounts(string arg1, string arg2, int arg3, ICreationScope arg4)
        {
            new VerifyErrorCounts().Verify(arg4, arg3, 2);
        }

        private void VerifyQueueCount(string arg1, string arg2, IBaseTransportOptions arg3, ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            new VerifyQueueRecordCount().Verify(arg4, arg5, false);
        }
    }
}
