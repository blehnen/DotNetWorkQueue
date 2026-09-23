using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Compatibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Compatibility
{
    /// <summary>
    /// <see cref="IntervalTimer"/> stands in for <c>PeriodicTimer</c> on netstandard2.0.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The cadence is the whole point, so it is what these test. A timer that delays for the period
    /// after each iteration passes a "does it tick" test and still puts back the defect #303 chased
    /// and #315 fixed, because the caller's work time is then added to every interval.
    /// </para>
    /// <para>
    /// The type is compiled on every target rather than only the one that needs it, which is what
    /// lets this suite - which runs on net10.0, where the framework has a real PeriodicTimer -
    /// exercise the code that netstandard2.0 will actually run (GitHub #252).
    /// </para>
    /// </remarks>
    [TestClass]
    public class IntervalTimerTests
    {
        private static readonly TimeSpan Period = TimeSpan.FromMilliseconds(100);

        [TestMethod]
        public async Task It_Ticks_On_The_Period()
        {
            using var timer = new IntervalTimer(Period);
            var start = Stopwatch.StartNew();

            for (var i = 0; i < 5; i++)
                Assert.IsTrue(await timer.WaitForNextTickAsync(CancellationToken.None));

            //loose on purpose - this one only establishes that five ticks cost about five periods.
            //The bound that separates a correct timer from a drifting one is in the next test.
            Assert.IsGreaterThanOrEqualTo(400, start.Elapsed.TotalMilliseconds,
                "five ticks of 100ms cannot have arrived early");
        }

        [TestMethod]
        public async Task Work_Inside_The_Period_Does_Not_Push_Later_Ticks_Out()
        {
            //the regression this type exists to avoid. Each iteration spends 40% of the period
            //working, which fits, so a timer measuring from a fixed point ticks at 100, 200, 300 ...
            //and ten ticks cost about a second. One that delays for the period after the work
            //instead adds 40ms to every interval, reaching about 1.4 seconds - and the gap grows
            //with the run rather than staying put, which is what made #303 hard to see.
            const int ticks = 10;
            var work = TimeSpan.FromMilliseconds(40);

            using var timer = new IntervalTimer(Period);
            var start = Stopwatch.StartNew();

            for (var i = 0; i < ticks; i++)
            {
                Assert.IsTrue(await timer.WaitForNextTickAsync(CancellationToken.None));
                await Task.Delay(work);
            }

            var elapsed = start.Elapsed.TotalMilliseconds;
            Assert.IsLessThanOrEqualTo(1200d, elapsed,
                $"ten 100ms ticks with 40ms of work each took {elapsed:F0}ms; a drifting timer takes about 1400ms");
        }

        [TestMethod]
        public async Task A_Caller_Slower_Than_The_Period_Loses_The_Ticks_It_Missed()
        {
            //a beat states what is true now, so firing the missed ones back to back afterwards
            //would be worse than dropping them. PeriodicTimer drops them; so does this.
            using var timer = new IntervalTimer(Period);

            Assert.IsTrue(await timer.WaitForNextTickAsync(CancellationToken.None));
            await Task.Delay(TimeSpan.FromMilliseconds(550));

            //the tick that was already due arrives at once
            var catchUp = Stopwatch.StartNew();
            Assert.IsTrue(await timer.WaitForNextTickAsync(CancellationToken.None));
            Assert.IsLessThanOrEqualTo(50d, catchUp.Elapsed.TotalMilliseconds,
                "a tick that came due while the caller was busy should not be waited for again");

            //and the one after it waits a full period rather than draining a backlog
            var next = Stopwatch.StartNew();
            Assert.IsTrue(await timer.WaitForNextTickAsync(CancellationToken.None));
            Assert.IsGreaterThanOrEqualTo(40d, next.Elapsed.TotalMilliseconds,
                "the five periods that were missed were replayed instead of dropped");
        }

        [TestMethod]
        public async Task Ticks_Stay_Aligned_To_The_Start_After_A_Slow_Caller()
        {
            //dropping the missed ticks must not shift the grid: the cadence is still measured from
            //when the timer was made, so a caller that recovers is back on the original beat.
            using var timer = new IntervalTimer(Period);
            var start = Stopwatch.StartNew();

            Assert.IsTrue(await timer.WaitForNextTickAsync(CancellationToken.None));
            await Task.Delay(TimeSpan.FromMilliseconds(250));

            var elapsed = new List<double>();
            for (var i = 0; i < 3; i++)
            {
                Assert.IsTrue(await timer.WaitForNextTickAsync(CancellationToken.None));
                elapsed.Add(start.Elapsed.TotalMilliseconds);
            }

            //the first of those is the catch-up and lands wherever the sleep ended; the two after it
            //are back on multiples of the period
            foreach (var at in elapsed.GetRange(1, 2))
            {
                var offGrid = at % Period.TotalMilliseconds;
                var fromGrid = Math.Min(offGrid, Period.TotalMilliseconds - offGrid);
                Assert.IsLessThanOrEqualTo(35d, fromGrid,
                    $"a tick at {at:F0}ms is {fromGrid:F0}ms off the 100ms grid the timer started on");
            }
        }

        [TestMethod]
        public async Task Disposing_Ends_The_Wait_Rather_Than_Throwing()
        {
            //the consumer's loop is `while (await WaitForNextTickAsync(...))`, so disposal has to
            //come back as false. Throwing would fault the loop on shutdown.
            var timer = new IntervalTimer(TimeSpan.FromSeconds(30));
            var waiting = timer.WaitForNextTickAsync(CancellationToken.None).AsTask();

            timer.Dispose();

            Assert.IsFalse(await waiting.WaitAsync(TimeSpan.FromSeconds(5)));
        }

        [TestMethod]
        public async Task Waiting_On_A_Disposed_Timer_Reports_False_Immediately()
        {
            var timer = new IntervalTimer(Period);
            timer.Dispose();

            Assert.IsFalse(await timer.WaitForNextTickAsync(CancellationToken.None));
        }

        [TestMethod]
        public async Task Disposing_While_A_Wait_Is_Starting_Still_Reports_False()
        {
            //the consumer's loop is `while (await WaitForNextTickAsync(...))`, so a wait that
            //throws on the way out faults shutdown rather than ending it. The window is between
            //the disposal check and the read of the token the wait links to, which cannot be
            //stepped through - it has to be raced.
            for (var attempt = 0; attempt < 500; attempt++)
            {
                var timer = new IntervalTimer(TimeSpan.FromMilliseconds(50));
                using var start = new ManualResetEventSlim(false);

                var waiting = Task.Run(async () =>
                {
                    start.Wait();
                    return await timer.WaitForNextTickAsync(CancellationToken.None);
                });
                var disposing = Task.Run(() =>
                {
                    start.Wait();
                    timer.Dispose();
                });

                start.Set();

                //a tick may win the race, so the result is not the assertion; not throwing is
                await Task.WhenAll(waiting, disposing);
            }
        }

        [TestMethod]
        public void Disposing_Twice_Is_Allowed()
        {
            var timer = new IntervalTimer(Period);
            timer.Dispose();
            timer.Dispose();
        }

        [TestMethod]
        public async Task The_Callers_Cancellation_Surfaces_To_The_Caller()
        {
            //unlike disposal, which the loop treats as a normal exit, a cancelled token is the
            //caller's own and has to be visible to them
            using var timer = new IntervalTimer(TimeSpan.FromSeconds(30));
            using var cancel = new CancellationTokenSource();
            var waiting = timer.WaitForNextTickAsync(cancel.Token).AsTask();

            cancel.Cancel();

            await Assert.ThrowsExactlyAsync<TaskCanceledException>(
                () => waiting.WaitAsync(TimeSpan.FromSeconds(5)));
        }

        [TestMethod]
        public void A_Period_That_Is_Not_Positive_Is_Refused()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new IntervalTimer(TimeSpan.Zero));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new IntervalTimer(TimeSpan.FromMilliseconds(-1)));
        }
    }
}
