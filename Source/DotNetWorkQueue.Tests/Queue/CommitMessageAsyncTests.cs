using System;
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.Queue
{
    /// <summary>
    /// <see cref="CommitMessage"/>'s awaitable member.
    ///
    /// Its job is to raise the event and to turn any failure underneath into a
    /// <see cref="CommitException"/> carrying the message's identity - which is what the queue's
    /// error handling is written against. `MessageProcessingAsync` catches `MessageException` to roll
    /// a message back, and `CommitException` is how a failed commit reaches that.
    /// </summary>
    [TestClass]
    public class CommitMessageAsyncTests
    {
        [TestMethod]
        public async Task CommitAsync_RaisesTheAwaitableEvent()
        {
            var context = Substitute.For<IMessageContext>();
            context.RaiseCommitAsync().Returns(Task.CompletedTask);

            var result = await new CommitMessage().CommitAsync(context).ConfigureAwait(false);

            Assert.IsTrue(result);
            await context.Received(1).RaiseCommitAsync().ConfigureAwait(false);
            context.DidNotReceive().RaiseCommit();
        }

        [TestMethod]
        public async Task CommitAsync_WrapsAFailureAsCommitException()
        {
            var context = ContextWithIdentity();
            context.RaiseCommitAsync()
                .Returns(Task.FromException(new InvalidOperationException("transport refused the delete")));

            var thrown = await Assert.ThrowsExactlyAsync<CommitException>(
                () => new CommitMessage().CommitAsync(context)).ConfigureAwait(false);

            Assert.IsInstanceOfType<InvalidOperationException>(thrown.InnerException);
        }

        [TestMethod]
        public async Task CommitAsync_TheExceptionCarriesTheMessageIdentity()
        {
            var context = ContextWithIdentity();
            context.RaiseCommitAsync().Returns(Task.FromException(new InvalidOperationException("boom")));

            var thrown = await Assert.ThrowsExactlyAsync<CommitException>(
                () => new CommitMessage().CommitAsync(context)).ConfigureAwait(false);

            //Without these the notification raised for a failed commit cannot say which message failed.
            Assert.AreSame(context.MessageId, thrown.MessageId);
            Assert.AreSame(context.CorrelationId, thrown.CorrelationId);
            Assert.AreSame(context.Headers, thrown.Headers);
        }

        [TestMethod]
        public async Task CommitAsync_WithNoContext_Throws()
        {
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => new CommitMessage().CommitAsync(null)).ConfigureAwait(false);
        }

        private static IMessageContext ContextWithIdentity()
        {
            var messageId = Substitute.For<IMessageId>();
            var correlationId = Substitute.For<ICorrelationId>();
            var context = Substitute.For<IMessageContext>();
            context.MessageId.Returns(messageId);
            context.CorrelationId.Returns(correlationId);
            return context;
        }
    }
}
