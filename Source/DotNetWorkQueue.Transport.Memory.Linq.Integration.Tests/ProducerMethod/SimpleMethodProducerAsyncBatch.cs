using System;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ProducerMethod
{
    [Collection("producer")]
    public class SimpleMethodProducerAsyncBatch
    {
        [Theory]
#if NETFULL
        [InlineData(1000, LinqMethodTypes.Dynamic),
        InlineData(1000, LinqMethodTypes.Compiled)]
#else
        [InlineData(1000, LinqMethodTypes.Compiled)]
#endif
        public async Task Run(
            int messageCount,
            LinqMethodTypes linqMethodTypes)
        {

            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation.
                        SimpleMethodProducerAsync();

                await consumer.Run<MemoryMessageQueueInit, MessageQueueCreation>(queueName,
                    connectionInfo.ConnectionString, messageCount, linqMethodTypes, false, false, true,
                    x => { }, Helpers.GenerateData, Verify).ConfigureAwait(false);
            }
        }

        private void Verify(QueueConnection arg1, QueueProducerConfiguration arg2, long arg3, ICreationScope arg4)
        {
            //noop
        }
    }
}
