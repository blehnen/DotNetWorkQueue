using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace DotNetWorkQueue.Tests.Queue
{
    /// <summary>
    /// Rolling back on the asynchronous consumer.
    ///
    /// `MessageProcessingAsync` rolls back from three separate catch paths, all of which run in a
    /// continuation on a thread-pool thread. Each is asserted, because a conversion that reaches two
    /// of the three leaves the third silently blocking - and the message is rolled back either way,
    /// so nothing else would notice.
    /// </summary>
    [TestClass]
    public class MessageProcessingAsyncRollbackTests
    {
        [TestMethod]
        public async Task Cancelled_RollsBackWithoutBlocking()
        {
            var harness = new Harness(new OperationCanceledException());

            await harness.Processor.HandleAsync().ConfigureAwait(false);

            await harness.Rollback.Received(1).RollbackAsync(Arg.Any<IMessageContext>()).ConfigureAwait(false);
            harness.Rollback.DidNotReceive().Rollback(Arg.Any<IMessageContext>());
        }

        [TestMethod]
        public async Task MessageException_RollsBackWithoutBlocking()
        {
            var harness = new Harness(new MessageException("bad", null, null, null));

            //The catch path rethrows, but RunAsync swallows MessageException on purpose - "nothing
            //else to do, but we want to avoid the general catch below" - so the worker loop is
            //released normally. What matters is that the rollback ran first.
            await harness.Processor.HandleAsync().ConfigureAwait(false);

            await harness.Rollback.Received(1).RollbackAsync(Arg.Any<IMessageContext>()).ConfigureAwait(false);
            harness.Rollback.DidNotReceive().Rollback(Arg.Any<IMessageContext>());
        }

        [TestMethod]
        public async Task UnexpectedException_RollsBackWithoutBlocking()
        {
            var harness = new Harness(new InvalidOperationException("boom"));

            await harness.Processor.HandleAsync().ConfigureAwait(false);

            await harness.Rollback.Received(1).RollbackAsync(Arg.Any<IMessageContext>()).ConfigureAwait(false);
            harness.Rollback.DidNotReceive().Rollback(Arg.Any<IMessageContext>());
        }

        [TestMethod]
        public async Task TheWorkerLoopWaitsForTheRollback()
        {
            var harness = new Harness(new OperationCanceledException());
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            harness.Rollback.RollbackAsync(Arg.Any<IMessageContext>()).Returns(gate.Task);

            var readyForNext = harness.Processor.HandleAsync();

            var completedEarly = await Task.WhenAny(readyForNext, Task.Delay(TimeSpan.FromMilliseconds(250)))
                .ConfigureAwait(false) == readyForNext;

            Assert.IsFalse(completedEarly,
                "The worker loop was released while the message was still being rolled back. It could " +
                "de-queue again - or shut down - before the transport had reset the message.");

            gate.TrySetResult(true);
            await readyForNext.ConfigureAwait(false);
        }

        private sealed class Harness
        {
            public MessageProcessingAsync Processor { get; }
            public IRollbackMessage Rollback { get; }

            public Harness(Exception thrownByReceive)
            {
                var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

                var receiveMessages = Substitute.For<IReceiveMessages>();
                //See MessageProcessingAsyncPacingTests: configuring the substitute is the one place the
                //returned ValueTask is not meant to be consumed, and CA2012 is an error repo-wide.
#pragma warning disable CA2012
                receiveMessages.ReceiveMessageAsync(Arg.Any<IMessageContext>(), Arg.Any<CancellationToken>())
                    .Throws(thrownByReceive);
#pragma warning restore CA2012

                var receiveFactory = Substitute.For<IReceiveMessagesFactory>();
                receiveFactory.Create().Returns(receiveMessages);

                Rollback = Substitute.For<IRollbackMessage>();
                Rollback.RollbackAsync(Arg.Any<IMessageContext>()).Returns(Task.FromResult(true));

                fixture.Inject(receiveFactory);
                fixture.Inject(Rollback);

                Processor = fixture.Create<MessageProcessingAsync>();
            }
        }
    }
}
