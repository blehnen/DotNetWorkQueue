using System;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.Memory.Tests.Basic
{
    /// <summary>
    /// The memory transport's asynchronous remove. Its task is completed rather than awaited work -
    /// messages live in process, so there is no I/O to release a thread for - which makes the risk a
    /// silent no-op rather than a blocked thread.
    /// </summary>
    [TestClass]
    public class RemoveMessageAsyncTests
    {
        [TestMethod]
        public async Task RemoveAsync_DeletesTheMessage()
        {
            var storage = Substitute.For<IDataStorage>();
            var id = Guid.NewGuid();
            storage.DeleteMessage(id).Returns(true);

            var status = await new RemoveMessage(storage)
                .RemoveAsync(MessageIdFor(id), RemoveMessageReason.Complete).ConfigureAwait(false);

            Assert.AreEqual(RemoveMessageStatus.Removed, status);
            storage.Received(1).DeleteMessage(id);
        }

        [TestMethod]
        public async Task RemoveAsync_WhenTheMessageIsGone_ReportsNotFound()
        {
            var storage = Substitute.For<IDataStorage>();
            var id = Guid.NewGuid();
            storage.DeleteMessage(id).Returns(false);

            var status = await new RemoveMessage(storage)
                .RemoveAsync(MessageIdFor(id), RemoveMessageReason.Complete).ConfigureAwait(false);

            Assert.AreEqual(RemoveMessageStatus.NotFound, status);
        }

        [TestMethod]
        public async Task RemoveAsync_FromAContext_UsesTheContextsMessageId()
        {
            var storage = Substitute.For<IDataStorage>();
            var id = Guid.NewGuid();
            storage.DeleteMessage(id).Returns(true);
            //Built first: creating substitutes inside a Returns() argument confuses NSubstitute's
            //last-call tracking and throws CouldNotSetReturnDueToNoLastCall.
            var messageId = MessageIdFor(id);
            var context = Substitute.For<IMessageContext>();
            context.MessageId.Returns(messageId);

            var status = await new RemoveMessage(storage)
                .RemoveAsync(context, RemoveMessageReason.Complete).ConfigureAwait(false);

            Assert.AreEqual(RemoveMessageStatus.Removed, status);
            storage.Received(1).DeleteMessage(id);
        }

        private static IMessageId MessageIdFor(Guid id)
        {
            var setting = Substitute.For<ISetting>();
            setting.Value.Returns(id);
            var messageId = Substitute.For<IMessageId>();
            messageId.HasValue.Returns(true);
            messageId.Id.Returns(setting);
            return messageId;
        }
    }
}
