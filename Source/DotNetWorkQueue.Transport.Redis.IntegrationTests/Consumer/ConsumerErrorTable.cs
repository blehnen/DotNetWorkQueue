using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Consumer
{
    [Collection("Consumer")]
    public class ConsumerErrorTable
    {
        [Theory]
        [InlineData(1, 120, 1, ConnectionInfoTypes.Linux),
         InlineData(100, 180, 20, ConnectionInfoTypes.Linux)]
        public void Run(int messageCount, int timeOut, int workerCount, ConnectionInfoTypes type)
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            var consumer = new DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.ConsumerErrorTable();

            consumer.Run<RedisQueueInit, FakeMessage, RedisQueueCreation>(queueName,
                connectionString,
                messageCount, timeOut, workerCount, false, x => { },
                Helpers.GenerateData, Helpers.Verify, VerifyQueueCount, ValidateErrorCounts);
        }

        private void ValidateErrorCounts(string arg1, string arg2, int arg3, ICreationScope arg4)
        {
            using (var error = new VerifyErrorCounts(arg1, arg2))
            {
                error.Verify(arg3, 2);
            }
        }

        private void VerifyQueueCount(string arg1, string arg2, IBaseTransportOptions arg3, ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            using (var count = new VerifyQueueRecordCount(arg1, arg2))
            {
                count.Verify(arg5, arg6, 2);
            }
        }
    }
}
