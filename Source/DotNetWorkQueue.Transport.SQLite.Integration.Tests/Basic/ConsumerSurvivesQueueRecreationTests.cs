using System;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Basic
{
    /// <summary>
    /// A running consumer keeps working after the queue it is attached to is dropped and created
    /// again. #355 refuses to build a consumer for a queue that does not exist, which does not
    /// cover a queue that goes away underneath one that is already running (GitHub #356).
    /// </summary>
    [TestClass]
    [Retry(1)]
    public class ConsumerSurvivesQueueRecreationTests
    {
        [TestMethod]
        public void AConsumerKeepsWorkingAfterTheQueueIsRecreated()
        {
            using var connectionInfo = new IntegrationConnectionInfo(false);
            var queueName = GenerateQueueName.Create();
            var queueConnection = new QueueConnection(queueName, connectionInfo.ConnectionString);

            using var queueCreator = new QueueCreationContainer<SqLiteMessageQueueInit>();
            var creation = queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection);
            try
            {
                Assert.IsTrue(creation.CreateQueue().Success);

                using var container = new QueueContainer<SqLiteMessageQueueInit>();
                var handled = 0;

                var receiveErrors = 0;

                using var consumer = container.CreateConsumer(queueConnection);
                consumer.Configuration.Worker.WorkerCount = 1;

                // Receive errors are counted, not ignored: whether a broken consumer complains or
                // goes quiet is the difference between noticing this and not.
                consumer.Start<FakeMessage>((message, notifications) => Interlocked.Increment(ref handled),
                    new ConsumerQueueNotifications(onReceiveMessageError:
                        notification => Interlocked.Increment(ref receiveErrors)));

                // Before anything is disturbed, to separate "never worked" from "stopped working".
                Send(container, queueConnection);
                Assert.IsTrue(WaitFor(() => Volatile.Read(ref handled) >= 1),
                    "the consumer never handled the first message, so the rest proves nothing");

                // Pin the count before disturbing anything, so a later increment cannot be
                // mistaken for the first message arriving late.
                Assert.AreEqual(1, Volatile.Read(ref handled));

                Assert.IsTrue(creation.RemoveQueue().Success);
                Assert.IsTrue(creation.CreateQueue().Success);

                Send(container, queueConnection);
                var recovered = WaitFor(() => Volatile.Read(ref handled) >= 2);
                Assert.IsTrue(recovered,
                    $"the consumer stopped handling messages once the queue was recreated; "
                    + $"handled={Volatile.Read(ref handled)}, receive errors={Volatile.Read(ref receiveErrors)}");
            }
            finally
            {
                creation.RemoveQueue();
                creation.Dispose();
            }
        }

        private static void Send(QueueContainer<SqLiteMessageQueueInit> container, QueueConnection queueConnection)
        {
            using var producer = container.CreateProducer<FakeMessage>(queueConnection);
            var result = producer.Send(new FakeMessage());
            Assert.IsFalse(result.HasError, result.SendingException?.ToString());
        }

        private static bool WaitFor(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (DateTime.UtcNow < deadline)
            {
                if (condition())
                    return true;

                Thread.Sleep(100);
            }

            return condition();
        }
    }
}
