using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Notifications;
using DotNetWorkQueue.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace DotNetWorkQueue.Tests.Queue
{
    /// <summary>
    /// The poison-message path on the asynchronous consumer.
    ///
    /// Moving a message to the error queue is transport I/O. Before #284 the asynchronous consumer
    /// called the synchronous <see cref="IReceivePoisonMessage.Handle"/> for it, which was free while
    /// the receive was a blocking call on the worker's own dedicated thread. Once the receive became
    /// awaitable this catch block runs in a continuation on a thread-pool thread, where that same
    /// call blocks one of the threads the consumer needs.
    ///
    /// Two things are asserted, because only the pair of them rules out a half-conversion: that the
    /// asynchronous member is the one called, and that its task is actually awaited. Calling
    /// <c>HandleAsync</c> and discarding the task would satisfy the first on its own while quietly
    /// letting the queue carry on before the message had been moved.
    /// </summary>
    [TestClass]
    public class MessageProcessingAsyncPoisonTests
    {
        private static readonly TimeSpan SettleTime = TimeSpan.FromMilliseconds(250);

        [TestMethod]
        public async Task PoisonMessage_MovesThroughTheAsynchronousHandler()
        {
            var harness = new Harness();

            var readyForNext = harness.Processor.HandleAsync();
            harness.CompleteTheMove();
            await readyForNext.ConfigureAwait(false);

            await harness.PoisonHandler.Received(1)
                .HandleAsync(Arg.Any<IMessageContext>(), Arg.Any<PoisonMessageException>())
                .ConfigureAwait(false);
            harness.PoisonHandler.DidNotReceive()
                .Handle(Arg.Any<IMessageContext>(), Arg.Any<PoisonMessageException>());
        }

        [TestMethod]
        public async Task PoisonMessage_TheWorkerLoopWaitsForTheMoveToFinish()
        {
            var harness = new Harness();

            var readyForNext = harness.Processor.HandleAsync();

            var completedEarly = await Task.WhenAny(readyForNext, Task.Delay(SettleTime))
                .ConfigureAwait(false) == readyForNext;

            Assert.IsFalse(completedEarly,
                "The worker loop was released while the message was still being moved to the error " +
                "queue. Discarding that task lets the consumer de-queue again - and shut down - " +
                "before the move has happened.");

            harness.CompleteTheMove();
            await readyForNext.ConfigureAwait(false);
        }

        [TestMethod]
        public async Task PoisonMessage_NotifiesOnlyAfterTheMoveHasFinished()
        {
            var harness = new Harness();

            var readyForNext = harness.Processor.HandleAsync();
            await Task.Delay(SettleTime).ConfigureAwait(false);

            harness.ErrorNotification.DidNotReceive()
                .InvokePoisonMessageError(Arg.Any<PoisonMessageNotification>());

            harness.CompleteTheMove();
            await readyForNext.ConfigureAwait(false);

            harness.ErrorNotification.Received(1)
                .InvokePoisonMessageError(Arg.Any<PoisonMessageNotification>());
        }

        private sealed class Harness
        {
            private readonly TaskCompletionSource<bool> _move =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            public MessageProcessingAsync Processor { get; }
            public IReceivePoisonMessage PoisonHandler { get; }
            public IConsumerQueueErrorNotification ErrorNotification { get; }

            public Harness()
            {
                var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

                var receiveMessages = Substitute.For<IReceiveMessages>();
                SetupPoisonReceive(receiveMessages);

                var receiveFactory = Substitute.For<IReceiveMessagesFactory>();
                receiveFactory.Create().Returns(receiveMessages);

                PoisonHandler = Substitute.For<IReceivePoisonMessage>();
                PoisonHandler.HandleAsync(Arg.Any<IMessageContext>(), Arg.Any<PoisonMessageException>())
                    .Returns(_ => _move.Task);

                ErrorNotification = Substitute.For<IConsumerQueueErrorNotification>();

                fixture.Inject(receiveFactory);
                fixture.Inject(PoisonHandler);
                fixture.Inject(ErrorNotification);

                Processor = fixture.Create<MessageProcessingAsync>();
            }

            public void CompleteTheMove() => _move.TrySetResult(true);

            private static void SetupPoisonReceive(IReceiveMessages receiveMessages)
            {
                //See MessageProcessingAsyncPacingTests: configuring the substitute is the one place
                //the returned ValueTask is not meant to be consumed, and CA2012 is an error repo-wide.
#pragma warning disable CA2012
                receiveMessages.ReceiveMessageAsync(Arg.Any<IMessageContext>(), Arg.Any<CancellationToken>())
                    .Throws(new PoisonMessageException());
#pragma warning restore CA2012
            }
        }
    }
}
