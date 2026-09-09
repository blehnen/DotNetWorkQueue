using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    /// <summary>
    /// Redis' asynchronous remove. Redis is the transport this work started from: its synchronous calls
    /// go through StackExchange.Redis' sync-over-async completions, which is the contention #161
    /// measured, so which handler runs is the whole point rather than an implementation detail.
    /// </summary>
    [TestClass]
    public class RemoveMessageAsyncTests
    {
        [TestMethod]
        public async Task RemoveAsync_UsesTheAsynchronousDeleteHandler()
        {
            var harness = new Harness(deleted: true);

            var status = await harness.Sut.RemoveAsync(harness.MessageId, RemoveMessageReason.Complete)
                .ConfigureAwait(false);

            Assert.AreEqual(RemoveMessageStatus.Removed, status);
            await harness.Async.Received(1).HandleAsync(Arg.Any<DeleteMessageCommand<string>>()).ConfigureAwait(false);
            harness.Sync.DidNotReceiveWithAnyArgs().Handle(null);
        }

        [TestMethod]
        public async Task RemoveAsync_WhenNothingWasDeleted_ReportsNotFound()
        {
            var harness = new Harness(deleted: false);

            var status = await harness.Sut.RemoveAsync(harness.MessageId, RemoveMessageReason.Expired)
                .ConfigureAwait(false);

            Assert.AreEqual(RemoveMessageStatus.NotFound, status);
        }

        [TestMethod]
        public async Task RemoveAsync_WithNoMessageId_ReportsNotFoundWithoutTouchingRedis()
        {
            var harness = new Harness(deleted: true, hasMessageId: false);

            var status = await harness.Sut.RemoveAsync(harness.MessageId, RemoveMessageReason.Complete)
                .ConfigureAwait(false);

            Assert.AreEqual(RemoveMessageStatus.NotFound, status);
            await harness.Async.DidNotReceiveWithAnyArgs().HandleAsync(null).ConfigureAwait(false);
        }

        private sealed class Harness
        {
            public RemoveMessage Sut { get; }
            public ICommandHandlerWithOutput<DeleteMessageCommand<string>, bool> Sync { get; }
            public ICommandHandlerWithOutputAsync<DeleteMessageCommand<string>, bool> Async { get; }
            public IMessageId MessageId { get; }

            public Harness(bool deleted, bool hasMessageId = true)
            {
                Sync = Substitute.For<ICommandHandlerWithOutput<DeleteMessageCommand<string>, bool>>();
                Async = Substitute.For<ICommandHandlerWithOutputAsync<DeleteMessageCommand<string>, bool>>();
                Async.HandleAsync(Arg.Any<DeleteMessageCommand<string>>()).Returns(Task.FromResult(deleted));

                var setting = Substitute.For<ISetting>();
                setting.Value.Returns("a-message");
                MessageId = Substitute.For<IMessageId>();
                MessageId.HasValue.Returns(hasMessageId);
                MessageId.Id.Returns(setting);

                Sut = new RemoveMessage(Sync, Async);
            }
        }
    }
}
