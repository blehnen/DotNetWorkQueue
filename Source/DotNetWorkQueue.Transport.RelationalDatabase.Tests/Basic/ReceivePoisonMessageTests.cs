using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    /// <summary>
    /// The shared poison-message handler every relational transport resolves.
    ///
    /// It holds both the synchronous and the asynchronous command handler, and the two paths must
    /// stay separate: the asynchronous consumer calling the synchronous handler is the defect #284
    /// exists to remove, and it would be invisible - the message still reaches the error queue
    /// either way - so it is asserted directly.
    /// </summary>
    [TestClass]
    public class ReceivePoisonMessageTests
    {
        [TestMethod]
        public async Task HandleAsync_UsesTheAsynchronousCommandHandler()
        {
            var harness = new Harness();

            await harness.Sut.HandleAsync(harness.Context, harness.Exception).ConfigureAwait(false);

            await harness.Async.Received(1).HandleAsync(Arg.Any<MoveRecordToErrorQueueCommand<long>>())
                .ConfigureAwait(false);
            harness.Sync.DidNotReceiveWithAnyArgs().Handle(null);
        }

        [TestMethod]
        public async Task HandleAsync_CarriesTheMessageIdAndException()
        {
            var harness = new Harness();

            await harness.Sut.HandleAsync(harness.Context, harness.Exception).ConfigureAwait(false);

            await harness.Async.Received(1).HandleAsync(Arg.Is<MoveRecordToErrorQueueCommand<long>>(
                    c => c.QueueId == Harness.MessageId && ReferenceEquals(c.Exception, harness.Exception)))
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task HandleAsync_ClearsTheMessageOnceTheMoveHasFinished()
        {
            var harness = new Harness();

            await harness.Sut.HandleAsync(harness.Context, harness.Exception).ConfigureAwait(false);

            harness.Context.Received(1)
                .SetMessageAndHeaders(null, harness.Context.CorrelationId, harness.Context.Headers);
        }

        [TestMethod]
        public async Task HandleAsync_WithNoMessageId_DoesNothing()
        {
            var harness = new Harness(hasMessageId: false);

            await harness.Sut.HandleAsync(harness.Context, harness.Exception).ConfigureAwait(false);

            await harness.Async.DidNotReceiveWithAnyArgs().HandleAsync(null).ConfigureAwait(false);
            harness.Context.DidNotReceiveWithAnyArgs().SetMessageAndHeaders(null, null, null);
        }

        private sealed class Harness
        {
            public const long MessageId = 42L;

            public ReceivePoisonMessage<long> Sut { get; }
            public ICommandHandler<MoveRecordToErrorQueueCommand<long>> Sync { get; }
            public ICommandHandlerAsync<MoveRecordToErrorQueueCommand<long>> Async { get; }
            public IMessageContext Context { get; }
            public PoisonMessageException Exception { get; }

            public Harness(bool hasMessageId = true)
            {
                Sync = Substitute.For<ICommandHandler<MoveRecordToErrorQueueCommand<long>>>();
                Async = Substitute.For<ICommandHandlerAsync<MoveRecordToErrorQueueCommand<long>>>();
                Exception = new PoisonMessageException();

                var setting = Substitute.For<ISetting>();
                setting.Value.Returns(MessageId);

                var messageId = Substitute.For<IMessageId>();
                messageId.HasValue.Returns(hasMessageId);
                messageId.Id.Returns(setting);

                Context = Substitute.For<IMessageContext>();
                Context.MessageId.Returns(messageId);

                Sut = new ReceivePoisonMessage<long>(Sync, Async);
            }
        }
    }
}
