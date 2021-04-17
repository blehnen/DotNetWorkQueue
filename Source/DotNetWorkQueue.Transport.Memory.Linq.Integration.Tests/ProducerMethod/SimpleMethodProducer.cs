using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ProducerMethod
{
    [Collection("producer")]
    public class SimpleMethodProducer
    {
        [Theory]
#if NETFULL
        [InlineData(1000, LinqMethodTypes.Dynamic),
         InlineData(100, LinqMethodTypes.Compiled)]
#else
        [InlineData(100, LinqMethodTypes.Compiled)]
#endif
        public void Run(
            int messageCount,
            LinqMethodTypes linqMethodTypes)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation.SimpleMethodProducer();

                consumer.Run<MemoryMessageQueueInit, MessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString), messageCount, linqMethodTypes, false, false, false,
                    x => { }, Helpers.GenerateData, Verify);
            }
        }

        private void Verify(QueueConnection arg1, QueueProducerConfiguration arg2, long arg3, ICreationScope arg4)
        {
            //noop
        }
    }
}
