using System;
using System.Threading.Tasks;
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
    public class SimpleConsumerAsync
    {
        [Theory]
        [InlineData(10, 5, 60, 7, 1, 1, 1),
         InlineData(100, 0, 30, 10, 5, 0, 1)]
        public async Task Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
           int messageType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.SimpleConsumerAsync();
                await consumer.Run<MemoryMessageQueueInit, MessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString),
                    messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, false, x => { },
                    Helpers.GenerateData, Helpers.Verify, VerifyQueueCount).ConfigureAwait(false);
            }
        }

        private void VerifyQueueCount(QueueConnection queueConnection, IBaseTransportOptions arg3, ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            new VerifyQueueRecordCount().Verify(arg4, 0, true);
        }
    }
}
