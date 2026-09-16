using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Basic
{
    /// <summary>
    /// A producer or consumer is refused if the queue it names has not been created.
    ///
    /// The library never required the queue to exist first, and what happened when it did not was
    /// neither an error nor recovery. On SQL Server and PostgreSQL every de-queue logged a transport
    /// error until the queue appeared; on SQLite and LiteDb the consumer never recovered at all, and
    /// LiteDb logged nothing whatsoever - a consumer that looked healthy and processed nothing for as
    /// long as it ran. Refusing at creation is the last point the caller can still do something about
    /// it (GitHub #348).
    /// </summary>
    [TestClass]
    [Retry(1)]
    public class QueueMustExistTests
    {
        [TestMethod]
        public void AConsumerForAQueueThatDoesNotExist_IsRefused()
        {
            using var connectionInfo = new IntegrationConnectionInfo(false);
            var queueName = GenerateQueueName.Create();
            var queueConnection = new QueueConnection(queueName, connectionInfo.ConnectionString);

            using var container = new QueueContainer<SqLiteMessageQueueInit>();

            var ex = Assert.ThrowsExactly<QueueDoesNotExistException>(
                () => container.CreateConsumer(queueConnection));

            //the message has to name the queue, or it says nothing the caller can act on
            Assert.Contains(queueName, ex.Message);
        }

        [TestMethod]
        public void AProducerForAQueueThatDoesNotExist_IsRefused()
        {
            using var connectionInfo = new IntegrationConnectionInfo(false);
            var queueName = GenerateQueueName.Create();
            var queueConnection = new QueueConnection(queueName, connectionInfo.ConnectionString);

            using var container = new QueueContainer<SqLiteMessageQueueInit>();

            Assert.ThrowsExactly<QueueDoesNotExistException>(
                () => container.CreateProducer<FakeMessage>(queueConnection));
        }

        [TestMethod]
        public void OnceTheQueueExists_NeitherIsRefused()
        {
            using var connectionInfo = new IntegrationConnectionInfo(false);
            var queueName = GenerateQueueName.Create();
            var queueConnection = new QueueConnection(queueName, connectionInfo.ConnectionString);

            using (var queueCreator = new QueueCreationContainer<SqLiteMessageQueueInit>())
            {
                var oCreation = queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection);
                try
                {
                    var created = oCreation.CreateQueue();
                    Assert.IsTrue(created.Success, created.ErrorMessage);

                    using var container = new QueueContainer<SqLiteMessageQueueInit>();
                    using var consumer = container.CreateConsumer(queueConnection);
                    using var producer = container.CreateProducer<FakeMessage>(queueConnection);
                    Assert.IsNotNull(consumer);
                    Assert.IsNotNull(producer);
                }
                finally
                {
                    oCreation.RemoveQueue();
                    oCreation.Dispose();
                }
            }
        }
    }
}
