using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.Basic
{
    /// <summary>
    /// Memory is exempt from the check a producer or consumer makes that its queue exists.
    ///
    /// There is nothing to create - an in-memory queue exists as soon as the container does - so
    /// applying the check would refuse every consumer of it. The same reasoning as Redis, for a
    /// different reason: Redis has a store with no schema, this has no store at all (GitHub #348).
    /// </summary>
    [TestClass]
    [Retry(1)]
    public class QueueNeedNotExistTests
    {
        [TestMethod]
        public void AConsumerAndProducerForAQueueNeverCreated_AreNotRefused()
        {
            using var connectionInfo = new IntegrationConnectionInfo();
            var queueConnection = new QueueConnection(GenerateQueueName.Create(), connectionInfo.ConnectionString);

            using var container = new QueueContainer<MemoryMessageQueueInit>();

            //nothing has created this queue
            using var consumer = container.CreateConsumer(queueConnection);
            using var producer = container.CreateProducer<FakeMessage>(queueConnection);

            Assert.IsNotNull(consumer);
            Assert.IsNotNull(producer);
        }
    }
}
