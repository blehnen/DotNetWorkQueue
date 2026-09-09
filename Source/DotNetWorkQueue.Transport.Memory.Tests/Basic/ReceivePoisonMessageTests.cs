using System;
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.Memory.Tests.Basic
{
    /// <summary>
    /// The memory transport's poison-message handler.
    ///
    /// Its asynchronous member is deliberately a completed task around the synchronous work - there
    /// is no I/O here to release a thread for - so what is worth pinning is that it still does the
    /// work rather than becoming a silent no-op, and that it still declines a context with no
    /// message id.
    /// </summary>
    [TestClass]
    public class ReceivePoisonMessageTests
    {
        [TestMethod]
        public async Task HandleAsync_MovesTheMessageToTheErrorQueue()
        {
            var storage = Substitute.For<IDataStorage>();
            var id = Guid.NewGuid();
            var context = ContextWith(id);
            var exception = new PoisonMessageException();

            await new ReceivePoisonMessage(storage).HandleAsync(context, exception).ConfigureAwait(false);

            storage.Received(1).MoveToErrorQueue(exception, id, context);
            context.Received(1).SetMessageAndHeaders(null, context.CorrelationId, context.Headers);
        }

        [TestMethod]
        public async Task HandleAsync_WithNoMessageId_DoesNothing()
        {
            var storage = Substitute.For<IDataStorage>();
            var context = Substitute.For<IMessageContext>();
            var messageId = Substitute.For<IMessageId>();
            messageId.HasValue.Returns(false);
            context.MessageId.Returns(messageId);

            await new ReceivePoisonMessage(storage).HandleAsync(context, new PoisonMessageException())
                .ConfigureAwait(false);

            storage.DidNotReceiveWithAnyArgs().MoveToErrorQueue(null, Guid.Empty, null);
        }

        private static IMessageContext ContextWith(Guid id)
        {
            var setting = Substitute.For<ISetting>();
            setting.Value.Returns(id);

            var messageId = Substitute.For<IMessageId>();
            messageId.HasValue.Returns(true);
            messageId.Id.Returns(setting);

            var context = Substitute.For<IMessageContext>();
            context.MessageId.Returns(messageId);
            return context;
        }
    }
}
