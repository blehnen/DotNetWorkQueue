using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ProducerMethod
{
    /// <summary>
    /// Exercises the producer method queue with message interceptors (gzip + triple DES encryption).
    /// This hits a different code path in the shared producer setup when interceptors are enabled.
    /// </summary>
    [TestClass]
    public class SimpleMethodProducerInterceptor
    {
        [TestMethod]
        [DataRow(100, LinqMethodTypes.Compiled)]
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
                        connectionInfo.ConnectionString), messageCount, linqMethodTypes, true, false, false,
                    x => { }, Helpers.GenerateData, Verify);
            }
        }

        private void Verify(QueueConnection arg1, QueueProducerConfiguration arg2, long arg3, ICreationScope arg4)
        {
            //noop
        }
    }
}
