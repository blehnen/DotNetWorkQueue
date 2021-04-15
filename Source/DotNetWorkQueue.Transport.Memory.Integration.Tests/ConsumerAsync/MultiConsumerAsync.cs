using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.ConsumerAsync
{
    [Collection("consumerasync")]
    public class MultiConsumerAsync
    {
        [Theory]
        [InlineData(10, 5, 65, 10, 1, 2),
         InlineData(10, 8, 60, 7, 1, 1),
         InlineData(100, 0, 30, 10, 5, 0)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.MultiConsumerAsync();
                consumer.Run<MemoryMessageQueueInit, MessageQueueCreation>(queueName,
                    connectionInfo.ConnectionString,
                    messageCount, runtime, timeOut, workerCount, readerCount, queueSize, false, x => { },
                    Helpers.GenerateData, Helpers.Verify, VerifyQueueCount);
            }
        }

        private void VerifyQueueCount(string arg1, string arg2, IBaseTransportOptions arg3, ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            //noop
        }
    }
}