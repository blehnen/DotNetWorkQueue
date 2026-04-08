using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.Memory.Basic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ProducerMethod
{
    [TestClass]
    public class SimpleMethodProducerAsyncBatch
    {
        [TestMethod]
        [DataRow(1000)]
        public async Task Run(
            int messageCount)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation.
                        SimpleMethodProducerAsync();

                await consumer.Run<MemoryMessageQueueInit, MessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString), messageCount, false, false, true,
                    x => { }, Helpers.GenerateData, Verify);
            }
        }

        private void Verify(QueueConnection arg1, QueueProducerConfiguration arg2, long arg3, ICreationScope arg4)
        {
            //noop
        }
    }
}
