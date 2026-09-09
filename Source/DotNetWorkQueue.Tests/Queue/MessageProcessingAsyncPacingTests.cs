using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.Queue
{
    /// <summary>
    /// Pins what paces the worker loop, which is the thing that bounds how many de-queues can be in
    /// flight at once.
    ///
    /// <c>Worker.MainLoop</c> is <c>while (!ShouldExit) { ... MessageProcessing.HandleAsync() ... }</c>
    /// and it waits on the returned task. So the invariant is simply: that task must not complete
    /// while a de-queue is still running. Until #256 the cap was an accident of the receive being a
    /// blocking call; awaiting the receive without moving the cap first was measured at 15,910,404
    /// concurrent in-flight de-queues against a configured worker count of seven, which OOM-killed
    /// the machine.
    ///
    /// These are deliberately unit tests over the pacing contract rather than a load test. A load
    /// test for this has to be bounded very carefully or it reproduces the OOM instead of reporting
    /// it; asserting the contract costs milliseconds and cannot run away.
    ///
    /// Every transport implementation added in the first half of #256 returns an already-completed
    /// <c>ValueTask</c>, so nothing here would fail without a receive that genuinely suspends. That
    /// is exactly why these tests supply one: the defect is invisible to every other test in the
    /// repository until a real transport goes async.
    /// </summary>
    [TestClass]
    public class MessageProcessingAsyncPacingTests
    {
        private static readonly TimeSpan SettleTime = TimeSpan.FromMilliseconds(250);

        [TestMethod]
        public async Task ReadyForNext_DoesNotComplete_WhileTheDeQueueIsStillRunning()
        {
            var harness = new Harness();
            var readyForNext = harness.Processor.HandleAsync();

            // The receive is pending, so the worker loop must still be parked here. If this task were
            // complete, MainLoop would already be starting another de-queue - the runaway.
            var completedEarly = await Task.WhenAny(readyForNext, Task.Delay(SettleTime))
                .ConfigureAwait(false) == readyForNext;

            Assert.IsFalse(completedEarly,
                "HandleAsync signalled the worker loop while the de-queue was still in flight. " +
                "MainLoop would immediately start another receive, and another, without bound.");

            harness.CompleteReceiveWith(null);
            await readyForNext.ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ReadyForNext_Completes_OnceTheDeQueueFindsNothing()
        {
            var harness = new Harness();
            var readyForNext = harness.Processor.HandleAsync();

            harness.CompleteReceiveWith(null);

            // The idle back-off is a substitute here, so it returns immediately; the point is that the
            // loop is released rather than left parked for good.
            var released = await Task.WhenAny(readyForNext, Task.Delay(SettleTime))
                .ConfigureAwait(false) == readyForNext;

            Assert.IsTrue(released,
                "HandleAsync never signalled the worker loop after an empty de-queue. A lost signal " +
                "here parks that worker permanently.");
        }

        [TestMethod]
        public async Task ReadyForNext_Completes_AfterDispatch_WithoutWaitingForTheWorkToFinish()
        {
            var harness = new Harness();
            var readyForNext = harness.Processor.HandleAsync();

            harness.CompleteReceiveWith(Substitute.For<IReceivedMessageInternal>());

            // Dispatched but still running: this is the old `async void Handle` hand-back point, and
            // the loop resuming here is what lets the scheduler run more work than there are workers.
            // Waiting for the work itself would quietly serialise each worker to one message at a time.
            var released = await Task.WhenAny(readyForNext, Task.Delay(SettleTime))
                .ConfigureAwait(false) == readyForNext;

            Assert.IsTrue(released,
                "HandleAsync waited for the message to finish processing before releasing the worker " +
                "loop. That serialises each worker to one message at a time and throws away the " +
                "point of the async consumer.");

            Assert.IsTrue(harness.HandlerWasInvoked,
                "The loop was released without the message having been dispatched.");

            harness.CompleteTheWork();
        }

        private sealed class Harness
        {
            private readonly TaskCompletionSource<IReceivedMessageInternal> _receive =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            private readonly TaskCompletionSource<bool> _work =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            public MessageProcessingAsync Processor { get; }
            public bool HandlerWasInvoked { get; private set; }

            public Harness()
            {
                var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

                var receiveMessages = Substitute.For<IReceiveMessages>();
                SetupReceive(receiveMessages, _receive.Task);

                var receiveFactory = Substitute.For<IReceiveMessagesFactory>();
                receiveFactory.Create().Returns(receiveMessages);

                var handler = Substitute.For<IHandleMessage>();
                handler.HandleAsync(Arg.Any<IReceivedMessageInternal>(), Arg.Any<IWorkerNotification>())
                    .Returns(_ =>
                    {
                        HandlerWasInvoked = true;
                        return _work.Task;
                    });

                fixture.Inject(receiveFactory);
                fixture.Inject(handler);

                Processor = fixture.Create<MessageProcessingAsync>();
            }

            public void CompleteReceiveWith(IReceivedMessageInternal message) => _receive.TrySetResult(message);

            public void CompleteTheWork() => _work.TrySetResult(true);

            private static void SetupReceive(IReceiveMessages receiveMessages, Task<IReceivedMessageInternal> pending)
            {
                //Configuring a substitute is the one place the returned ValueTask is not meant to be
                //consumed - NSubstitute intercepts the call to record the setup - and CA2012 is an
                //error repo-wide, flagging it whether it is a receiver or an argument.
#pragma warning disable CA2012
                receiveMessages.ReceiveMessageAsync(Arg.Any<IMessageContext>(), Arg.Any<CancellationToken>())
                    .Returns(new ValueTask<IReceivedMessageInternal>(pending));
#pragma warning restore CA2012
            }
        }
    }
}
