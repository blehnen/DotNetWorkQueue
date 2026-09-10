using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.Messages
{
    /// <summary>
    /// The awaitable rollback event, the twin of the commit one added in #295.
    ///
    /// Rolling back is transport I/O — resetting the heartbeat and delay, or rolling back a held
    /// transaction — and on the asynchronous consumer it runs from a catch path in a continuation on a
    /// thread-pool thread.
    /// </summary>
    [TestClass]
    public class MessageContextRollbackAsyncTests
    {
        private static readonly string[] SequentialOrder = { "first-start", "first-end", "second-start" };

        [TestMethod]
        public async Task RaiseRollbackAsync_WithNoSubscribers_DoesNothing()
        {
            using var context = NewContext();

            await context.RaiseRollbackAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RaiseRollbackAsync_AwaitsTheSubscriber()
        {
            using var context = NewContext();
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            context.RollbackAsync += (_, _) => gate.Task;

            var raising = context.RaiseRollbackAsync();

            var completedEarly = await Task.WhenAny(raising, Task.Delay(TimeSpan.FromMilliseconds(250)))
                .ConfigureAwait(false) == raising;

            Assert.IsFalse(completedEarly,
                "The raise completed while the subscriber was still working. The consumer would carry " +
                "on - and could shut down - before the message had actually been rolled back.");

            gate.TrySetResult(true);
            await raising.ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RaiseRollbackAsync_RunsSubscribersOneAfterAnother()
        {
            using var context = NewContext();
            var order = new List<string>();

            context.RollbackAsync += async (_, _) =>
            {
                order.Add("first-start");
                await Task.Delay(30).ConfigureAwait(false);
                order.Add("first-end");
            };
            context.RollbackAsync += (_, _) =>
            {
                order.Add("second-start");
                return Task.CompletedTask;
            };

            await context.RaiseRollbackAsync().ConfigureAwait(false);

            CollectionAssert.AreEqual(SequentialOrder, order);
        }

        [TestMethod]
        public async Task RaiseRollbackAsync_DoesNotRaiseTheSynchronousEvent()
        {
            using var context = NewContext();
            var syncCalled = false;
            var asyncCalled = false;
            context.Rollback += (_, _) => syncCalled = true;
            context.RollbackAsync += (_, _) => { asyncCalled = true; return Task.CompletedTask; };

            await context.RaiseRollbackAsync().ConfigureAwait(false);

            Assert.IsTrue(asyncCalled);
            Assert.IsFalse(syncCalled,
                "A context raises one event or the other. Raising both would roll the message back twice.");
        }

        [TestMethod]
        public void RaiseRollback_DoesNotRaiseTheAwaitableEvent()
        {
            using var context = NewContext();
            var syncCalled = false;
            var asyncCalled = false;
            context.Rollback += (_, _) => syncCalled = true;
            context.RollbackAsync += (_, _) => { asyncCalled = true; return Task.CompletedTask; };

            context.RaiseRollback();

            Assert.IsTrue(syncCalled);
            Assert.IsFalse(asyncCalled);
        }

        [TestMethod]
        public async Task RaiseRollbackAsync_AfterDispose_Throws()
        {
            var context = NewContext();
            context.Dispose();

            await Assert.ThrowsExactlyAsync<ObjectDisposedException>(
                () => context.RaiseRollbackAsync()).ConfigureAwait(false);
        }

        private static MessageContext NewContext() =>
            new MessageContext(Substitute.For<IWorkerNotificationFactory>());
    }
}
