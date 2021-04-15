using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ConsumerMethodAsync
{
    [Collection("consumer")]
    public class SimpleMethodConsumerAsync
    {
        [Theory]
#if NETFULL
        [InlineData(10, 15, 60, 7, 1, 1, 1, LinqMethodTypes.Dynamic),
         InlineData(10, 5, 60, 10, 1, 2, 1, LinqMethodTypes.Compiled)]
#else
        [InlineData(10, 5, 60, 10, 1, 2, 1, LinqMethodTypes.Compiled)]
#endif
        public void Run(int messageCount, int runtime, int timeOut,
            int workerCount, int readerCount, int queueSize,
           int messageType, LinqMethodTypes linqMethodTypes)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync.Implementation.
                        SimpleMethodConsumerAsync();

                consumer.Run<MemoryMessageQueueInit, MessageQueueCreation>(queueName,
                    connectionInfo.ConnectionString,
                    messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, linqMethodTypes, false, x => { },
                    Helpers.GenerateData, Helpers.Verify, VerifyQueueCount);
            }
        }

        private void VerifyQueueCount(string arg1, string arg2, IBaseTransportOptions arg3, ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            new VerifyQueueRecordCount().Verify(arg4, 0, true);
        }
    }
}
