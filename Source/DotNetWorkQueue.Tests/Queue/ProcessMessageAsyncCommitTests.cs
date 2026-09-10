using System;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.Queue
{
    /// <summary>
    /// Committing on the asynchronous consumer.
    ///
    /// The commit is the busiest of the calls #284 tracks: it runs on every message, on the happy
    /// path, in a continuation on a thread-pool thread. Both the assertion that the awaitable member
    /// is used and the assertion that the blocking one is not are needed - the message is committed
    /// either way, so nothing else would notice the wrong one.
    /// </summary>
    [TestClass]
    public class ProcessMessageAsyncCommitTests
    {
        [TestMethod]
        public async Task HandleAsync_CommitsWithoutBlocking()
        {
            var harness = new Harness();

            await harness.Processor.HandleAsync(harness.Context, harness.Message).ConfigureAwait(false);

            await harness.CommitMessage.Received(1).CommitAsync(harness.Context).ConfigureAwait(false);
            harness.CommitMessage.DidNotReceive().Commit(Arg.Any<IMessageContext>());
        }

        [TestMethod]
        public async Task HandleAsync_WaitsForTheCommit()
        {
            var harness = new Harness();
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            harness.CommitMessage.CommitAsync(Arg.Any<IMessageContext>()).Returns(gate.Task);

            var handling = harness.Processor.HandleAsync(harness.Context, harness.Message);

            var completedEarly = await Task.WhenAny(handling, Task.Delay(TimeSpan.FromMilliseconds(250)))
                .ConfigureAwait(false) == handling;

            Assert.IsFalse(completedEarly,
                "Processing reported done while the commit was still in flight. The consumer would " +
                "release the message - and could shut down - before the transport had committed it.");

            gate.TrySetResult(true);
            await handling.ConfigureAwait(false);
        }

        [TestMethod]
        public async Task HandleAsync_WhenTheCommitFails_TheExceptionHandlerSeesIt()
        {
            var harness = new Harness();
            harness.CommitMessage.CommitAsync(Arg.Any<IMessageContext>())
                .Returns(Task.FromException<bool>(new InvalidOperationException("commit failed")));

            //A failed commit goes through the message exception handler, which wraps and rethrows -
            //so it surfaces as MessageException, which is what MessageProcessingAsync catches to roll
            //the message back. The awaited commit must not change that contract.
            var thrown = await Assert.ThrowsExactlyAsync<MessageException>(
                () => harness.Processor.HandleAsync(harness.Context, harness.Message)).ConfigureAwait(false);
            Assert.IsInstanceOfType<InvalidOperationException>(thrown.InnerException);
        }

        [TestMethod]
        public async Task HandleAsync_DoesNotCommitWhenTheHandlerThrows()
        {
            var harness = new Harness();
            harness.Handler
                .HandleAsync(Arg.Any<IReceivedMessageInternal>(), Arg.Any<IWorkerNotification>())
                .Returns(Task.FromException(new InvalidOperationException("handler failed")));

            await Assert.ThrowsExactlyAsync<MessageException>(
                () => harness.Processor.HandleAsync(harness.Context, harness.Message)).ConfigureAwait(false);

            await harness.CommitMessage.DidNotReceiveWithAnyArgs().CommitAsync(null).ConfigureAwait(false);
        }

        private sealed class Harness
        {
            public ProcessMessageAsync Processor { get; }
            public ICommitMessage CommitMessage { get; }
            public IHandleMessage Handler { get; }
            public IMessageContext Context { get; }
            public IReceivedMessageInternal Message { get; }

            public Harness()
            {
                var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

                CommitMessage = Substitute.For<ICommitMessage>();
                CommitMessage.CommitAsync(Arg.Any<IMessageContext>()).Returns(Task.FromResult(true));

                Handler = Substitute.For<IHandleMessage>();
                Handler.HandleAsync(Arg.Any<IReceivedMessageInternal>(), Arg.Any<IWorkerNotification>())
                    .Returns(Task.CompletedTask);

                fixture.Inject(CommitMessage);
                fixture.Inject(Handler);

                Context = Substitute.For<IMessageContext>();
                Message = Substitute.For<IReceivedMessageInternal>();
                Processor = fixture.Create<ProcessMessageAsync>();
            }
        }
    }
}
