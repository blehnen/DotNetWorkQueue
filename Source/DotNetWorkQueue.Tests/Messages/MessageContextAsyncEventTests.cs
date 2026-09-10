using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Messages
{
    /// <summary>
    /// The awaitable commit event on the message context.
    ///
    /// <see cref="EventHandler"/> returns void, so a subscriber doing transport I/O had to block the
    /// thread that raised the event - and on the asynchronous consumer that raise happens in a
    /// continuation on a thread-pool thread, once per message. The awaitable event is what lets the
    /// transports commit without holding it.
    ///
    /// Handlers run one after another rather than concurrently, which is what the synchronous event
    /// already did. That ordering is load-bearing: the transports commit against a shared connection
    /// and transaction, which cannot take concurrent work.
    /// </summary>
    [TestClass]
    public class MessageContextAsyncEventTests
    {
        [TestMethod]
        public async Task RaiseCommitAsync_WithNoSubscribers_DoesNothing()
        {
            using var context = NewContext();

            await context.RaiseCommitAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RaiseCommitAsync_AwaitsTheSubscriber()
        {
            using var context = NewContext();
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            context.CommitAsync += (_, _) => gate.Task;

            var raising = context.RaiseCommitAsync();

            var completedEarly = await Task.WhenAny(raising, Task.Delay(TimeSpan.FromMilliseconds(250)))
                .ConfigureAwait(false) == raising;

            Assert.IsFalse(completedEarly,
                "The raise completed while the subscriber was still working. Discarding that task lets " +
                "the consumer carry on before the message has actually been committed.");

            gate.TrySetResult(true);
            await raising.ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RaiseCommitAsync_RunsSubscribersOneAfterAnother()
        {
            using var context = NewContext();
            var order = new List<string>();

            context.CommitAsync += async (_, _) =>
            {
                order.Add("first-start");
                await Task.Delay(30).ConfigureAwait(false);
                order.Add("first-end");
            };
            context.CommitAsync += (_, _) =>
            {
                order.Add("second-start");
                return Task.CompletedTask;
            };

            await context.RaiseCommitAsync().ConfigureAwait(false);

            //Not WhenAll: the second handler must not start until the first has finished, because the
            //transports commit against a shared connection and transaction.
            CollectionAssert.AreEqual(new[] { "first-start", "first-end", "second-start" }, order);
        }

        [TestMethod]
        public async Task RaiseCommitAsync_PropagatesTheSubscribersFailure()
        {
            using var context = NewContext();
            context.CommitAsync += (_, _) => Task.FromException(new InvalidOperationException("commit failed"));

            //A failed commit must not be swallowed here - CommitMessage turns it into a CommitException,
            //which is what the queue's error handling is written against.
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => context.RaiseCommitAsync()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RaiseCommitAsync_AfterDispose_Throws()
        {
            var context = NewContext();
            context.Dispose();

            await Assert.ThrowsExactlyAsync<ObjectDisposedException>(
                () => context.RaiseCommitAsync()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RaiseCommitAsync_DoesNotRaiseTheSynchronousEvent()
        {
            using var context = NewContext();
            var syncCalled = false;
            var asyncCalled = false;
            context.Commit += (_, _) => syncCalled = true;
            context.CommitAsync += (_, _) => { asyncCalled = true; return Task.CompletedTask; };

            await context.RaiseCommitAsync().ConfigureAwait(false);

            Assert.IsTrue(asyncCalled);
            Assert.IsFalse(syncCalled,
                "A context raises one event or the other. Raising both would commit the message twice.");
        }

        [TestMethod]
        public void RaiseCommit_DoesNotRaiseTheAwaitableEvent()
        {
            using var context = NewContext();
            var syncCalled = false;
            var asyncCalled = false;
            context.Commit += (_, _) => syncCalled = true;
            context.CommitAsync += (_, _) => { asyncCalled = true; return Task.CompletedTask; };

            context.RaiseCommit();

            Assert.IsTrue(syncCalled);
            Assert.IsFalse(asyncCalled);
        }

        private static MessageContext NewContext() =>
            new MessageContext(Substitute.For<IWorkerNotificationFactory>());
    }
}
