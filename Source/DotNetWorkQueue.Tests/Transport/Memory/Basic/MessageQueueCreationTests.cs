using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic;
using DotNetWorkQueue.Transport.Memory.Basic.Factory;
using Xunit;

namespace DotNetWorkQueue.Tests.Transport.Memory.Basic
{
    public class MessageQueueCreationTests
    {
        [Fact()]
        public void MessageQueueCreation_Test()
        {
            var queue = Create();
            Assert.False(queue.IsDisposed);
            Assert.True(queue.QueueExists);
            Assert.NotNull(queue.ConnectionInfo);
            Assert.NotNull(queue.Options);
            Assert.NotNull(queue.Scope);

            var result = queue.CreateQueue();
            Assert.Equal(QueueCreationStatus.Success, result.Status);

            var delete = queue.RemoveQueue();
            Assert.Equal(QueueRemoveStatus.Success, delete.Status);


        }

        [Fact()]
        public void CreateQueue_Test()
        {
            var queue = Create();
            var result = queue.CreateQueue();
            Assert.Equal(QueueCreationStatus.Success, result.Status);
        }

        [Fact()]
        public void RemoveQueue_Test()
        {
            var queue = Create();
            var delete = queue.RemoveQueue();
            Assert.Equal(QueueRemoveStatus.Success, delete.Status);
        }

        [Fact()]
        public void Dispose_Test()
        {
            var queue = Create();
            Assert.False(queue.IsDisposed);
            queue.Dispose();
            Assert.True(queue.IsDisposed);
        }

        private MessageQueueCreation Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject<ITransportOptionsFactory>(new TransportOptionsFactory());
            return fixture.Create<MessageQueueCreation>();
        }
    }
}