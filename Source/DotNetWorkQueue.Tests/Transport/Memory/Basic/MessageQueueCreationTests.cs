using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic;
using DotNetWorkQueue.Transport.Memory.Basic.Factory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Transport.Memory.Basic
{
    [TestClass]
    public class MessageQueueCreationTests
    {
        [TestMethod]
        public void MessageQueueCreation_Test()
        {
            var queue = Create();
            Assert.IsFalse(queue.IsDisposed);
            Assert.IsTrue(queue.QueueExists);
            Assert.IsNotNull(queue.ConnectionInfo);
            Assert.IsNotNull(queue.Options);
            Assert.IsNotNull(queue.Scope);

            var result = queue.CreateQueue();
            Assert.AreEqual(QueueCreationStatus.Success, result.Status);

            var delete = queue.RemoveQueue();
            Assert.AreEqual(QueueRemoveStatus.Success, delete.Status);


        }

        [TestMethod]
        public void CreateQueue_Test()
        {
            var queue = Create();
            var result = queue.CreateQueue();
            Assert.AreEqual(QueueCreationStatus.Success, result.Status);
        }

        [TestMethod]
        public void RemoveQueue_Test()
        {
            var queue = Create();
            var delete = queue.RemoveQueue();
            Assert.AreEqual(QueueRemoveStatus.Success, delete.Status);
        }

        [TestMethod]
        public void Dispose_Test()
        {
            var queue = Create();
            Assert.IsFalse(queue.IsDisposed);
            queue.Dispose();
            Assert.IsTrue(queue.IsDisposed);
        }

        private MessageQueueCreation Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject<ITransportOptionsFactory>(new TransportOptionsFactory());
            return fixture.Create<MessageQueueCreation>();
        }
    }
}