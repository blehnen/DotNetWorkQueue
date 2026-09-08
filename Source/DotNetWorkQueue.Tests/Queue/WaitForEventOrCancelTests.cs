using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;


using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class WaitForEventOrCancelTests
    {
        [TestMethod]
        public void IsDisposed_False_By_Default()
        {
            using (var test = Create())
            {
                Assert.IsFalse(test.IsDisposed);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [TestMethod]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.IsTrue(test.IsDisposed);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [TestMethod]
        public void Dispose_Can_Be_Called_Multiple_Times()
        {
            using (var test = Create())
            {
                test.Dispose();
            }
        }

        [TestMethod]
        public void Cancel_IfDisposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
                    delegate
                    {
                        test.Cancel();
                    });
            }
        }

        [TestMethod]
        public void Reset_IfDisposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
                    delegate
                    {
                        test.Reset();
                    });
            }
        }

        [TestMethod]
        public void Wait_IfDisposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
                    delegate
                    {
                        test.Wait();
                    });
            }
        }
        [TestMethod]
        public void Set_IfDisposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
                    delegate
                    {
                        test.Set();
                    });
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void Wait_Set()
        {
            using (var test = Create())
            {
                test.Reset();
                Task.Factory.StartNew(() => { Thread.Sleep(1000); test.Set(); }, TaskCreationOptions.LongRunning);
                test.Wait();
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void Wait_Cancel()
        {
            using (var test = Create())
            {
                test.Reset();
                Task.Factory.StartNew(() => { Thread.Sleep(1000); test.Cancel(); }, TaskCreationOptions.LongRunning);
                test.Wait();
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void Wait_Set_Reset_Wait()
        {
            using (var test = Create())
            {
                test.Reset();
                Task.Factory.StartNew(() => { Thread.Sleep(1000); test.Set(); }, TaskCreationOptions.LongRunning);
                test.Wait();
                test.Reset();
                Task.Factory.StartNew(() => { Thread.Sleep(1000); test.Set(); }, TaskCreationOptions.LongRunning);
                test.Wait();
            }
        }

        [TestMethod]
        public async Task WaitAsync_ReturnsImmediately_WhenAlreadySignaled()
        {
            using var test = Create();
            // constructed signaled, per ManualResetEventSlim(true)
            Assert.IsTrue(await test.WaitAsync());
        }

        [TestMethod]
        public async Task WaitAsync_Blocks_UntilSet()
        {
            using var test = Create();
            test.Reset();

            var waiter = test.WaitAsync().AsTask();
            Assert.IsFalse(waiter.IsCompleted, "must not complete while reset");

            test.Set();
            Assert.IsTrue(await waiter);
        }

        [TestMethod]
        public async Task WaitAsync_ReturnsFalse_WhenCancelled()
        {
            using var test = Create();
            test.Reset();

            var waiter = test.WaitAsync().AsTask();
            test.Cancel();

            Assert.IsFalse(await waiter);
        }

        [TestMethod]
        public async Task WaitAsync_ResetDuringPendingWait_DoesNotOrphanTheWaiter()
        {
            //the lost wake-up case: Reset must not swap out a TCS that already has waiters,
            //or this waiter sleeps until some later Set that it was never told about
            using var test = Create();
            test.Reset();

            var waiter = test.WaitAsync().AsTask();
            test.Reset();                       // second reset, waiter already pending
            test.Set();

            var completed = await Task.WhenAny(waiter, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.AreSame(waiter, completed, "waiter was orphaned by Reset");
            Assert.IsTrue(await waiter);
        }

        [TestMethod]
        public async Task WaitAsync_SetThenReset_BeforeWaiterResumes_StillReleases()
        {
            using var test = Create();
            test.Reset();

            var waiter = test.WaitAsync().AsTask();
            test.Set();
            test.Reset();                       // immediately re-armed

            var completed = await Task.WhenAny(waiter, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.AreSame(waiter, completed, "a Set that happened must release its waiter");
            Assert.IsTrue(await waiter);
        }

        [TestMethod]
        public async Task WaitAsync_ManyWaiters_AllReleasedByOneSet()
        {
            //a real fan-out across distinct completion sources, not one Task awaited 50
            //times: Reset only swaps in a fresh source once the current one has completed,
            //so each earlier batch is released with Set() before the next batch is
            //acquired on the next source. Only the last batch is still pending when the
            //final Set() runs, and that final Set must release it.
            using var test = Create();
            test.Reset();

            const int batches = 5;
            const int perBatch = 10;
            var waiters = new Task<bool>[batches * perBatch];

            for (var batch = 0; batch < batches; batch++)
            {
                for (var i = 0; i < perBatch; i++)
                    waiters[batch * perBatch + i] = test.WaitAsync().AsTask();

                if (batch < batches - 1)
                {
                    test.Set();     // completes this batch's source
                    test.Reset();   // source is now completed, so this swaps in a new one
                }
            }

            // only the final batch is still pending at this point
            test.Set();

            var all = Task.WhenAll(waiters);
            var completed = await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.AreSame(all, completed, "the final Set must release every waiter still outstanding");
            foreach (var r in await all)
                Assert.IsTrue(r);
        }

        [TestMethod]
        public async Task WaitAsync_IfDisposed_Exception()
        {
            var test = Create();
            test.Dispose();
            await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await test.WaitAsync());
        }

        [TestMethod]
        public async Task WaitAsync_PendingWait_IsReleasedByDispose()
        {
            //an async waiter holds only a Task - disposing the primitives underneath does
            //not fault it, so without explicit completion this waits forever on a dead object
            var test = Create();
            test.Reset();
            var waiter = test.WaitAsync().AsTask();

            test.Dispose();

            var completed = await Task.WhenAny(waiter, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.AreSame(waiter, completed, "Dispose left a waiter hanging");
            Assert.IsFalse(await waiter);
        }

        [TestMethod]
        public async Task WaitAsync_AfterCancelThenReset_StillReturnsFalse()
        {
            //Cancel completes the source; Reset then sees "completed" and re-arms it. The
            //synchronous Wait would still return false because it consults the token, so the
            //async path must too, or it waits on an object that can never be signaled again.
            using var test = Create();
            test.Cancel();
            test.Reset();

            var waiter = test.WaitAsync().AsTask();
            var completed = await Task.WhenAny(waiter, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.AreSame(waiter, completed, "a cancelled instance must not block an async waiter");
            Assert.IsFalse(await waiter);
        }

        [TestMethod]
        public void SetAndReset_Concurrently_LeaveSyncAndAsyncAgreeing()
        {
            //The interleaving that lock-free code got wrong: Reset reads the completed source,
            //Set signals the event and that same source, then Reset installs a fresh incomplete
            //one - leaving the event signaled while the async source is not. A synchronous
            //waiter would proceed and an async waiter would hang.
            //
            //Wait() has no timeout overload, so it is probed on a task and released with Cancel()
            //rather than called directly - calling it unguarded would hang this test.
            string failure = null;

            for (var attempt = 0; attempt < 100 && failure == null; attempt++)
            {
                using var test = Create();
                using var start = new ManualResetEventSlim(false);

                var setter = Task.Run(() => { start.Wait(); test.Set(); });
                var resetter = Task.Run(() => { start.Wait(); test.Reset(); });

                start.Set();
                Task.WaitAll(setter, resetter);

                var syncProbe = Task.Run(() => test.Wait());
                try
                {
                    var syncSignaled = syncProbe.Wait(TimeSpan.FromMilliseconds(250)) && syncProbe.Result;
                    var asyncSignaled = test.WaitAsync().AsTask().Wait(TimeSpan.FromMilliseconds(250));

                    //the implication is what matters: the two views must not disagree in the
                    //direction that hangs a consumer
                    if (syncSignaled && !asyncSignaled)
                        failure = $"attempt {attempt}: reset event signaled but async waiter still pending";
                }
                finally
                {
                    //release the probe if it is still blocked, so it isn't left inside
                    //Wait() when the "using" disposes test - even on Assert.Fail/exception
                    test.Cancel();
                    syncProbe.Wait(TimeSpan.FromSeconds(1));
                }
            }

            if (failure != null)
                Assert.Fail(failure);
        }

        private IWaitForEventOrCancel Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<WaitForEventOrCancel>();
        }
    }
}
