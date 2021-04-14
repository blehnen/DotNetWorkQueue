using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.Consumer
{
    [Collection("consumer")]
    public class ConsumerErrorTable
    {
        [Theory]
        [InlineData(10, 20, 5)]
        public void Run(int messageCount, int timeOut, int workerCount)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var producer = new DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.ConsumerErrorTable();
                producer.Run<MemoryMessageQueueInit, FakeMessage, MessageQueueCreation>(queueName,
                    connectionInfo.ConnectionString,
                    messageCount, timeOut, workerCount, false, x => { },
                    Helpers.GenerateData, Helpers.Verify, VerifyQueueCount, ValidateErrorCounts);
            }
        }

        private void ValidateErrorCounts(string arg1, string arg2, int arg3, ICreationScope arg4)
        {
            new VerifyErrorCounts().Verify(arg4, arg3, 1);
        }

        private void VerifyQueueCount(string arg1, string arg2, IBaseTransportOptions arg3, ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            new VerifyQueueRecordCount().Verify(arg4, 0, true);
        }
    }
}
