using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Basic
{
    /// <summary>
    /// Redis is exempt from the check a producer or consumer makes that its queue exists.
    ///
    /// There is nothing to create: CreateQueue is a no-op, and QueueExists answers on content rather
    /// than structure, so a queue that is merely empty reads as absent. Applying the check here would
    /// refuse every correctly configured consumer whose queue happened to have no messages in it -
    /// which is every consumer, at the moment it starts (GitHub #348).
    /// </summary>
    [TestClass]
    [Retry(1)]
    public class QueueNeedNotExistTests
    {
        [TestMethod]
        public void AConsumerAndProducerForAnEmptyQueue_AreNotRefused()
        {
            var queueName = GenerateQueueName.Create();
            var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);

            using var container = new QueueContainer<RedisQueueInit>();

            //nothing has ever been written to this queue name
            using var consumer = container.CreateConsumer(queueConnection);
            using var producer = container.CreateProducer<FakeMessage>(queueConnection);

            Assert.IsNotNull(consumer);
            Assert.IsNotNull(producer);
        }
    }
}
