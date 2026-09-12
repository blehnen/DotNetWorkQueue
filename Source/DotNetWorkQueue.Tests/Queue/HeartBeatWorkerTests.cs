using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;


using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class HeartBeatWorkerTests
    {
        private static readonly TimeSpan DrainWindow = TimeSpan.FromMilliseconds(500);

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
        public void Call_Dispose_Multiple_Times_Ok()
        {
            using (var test = Create())
            {
                test.Dispose();
            }
        }

        [TestMethod]
        public void Call_Stop_Multiple_Times_Ok()
        {
            using (var test = Create())
            {
                test.Stop();
                test.Stop();
            }
        }

        [TestMethod]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = Create();
            test.Dispose();
            Assert.IsTrue(test.IsDisposed);
        }

        [TestMethod]
        public void Disposed_Instance_Start_Exception()
        {
            var test = Create();
            test.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(
                delegate
                {
                    test.Start();
                });
        }

        [TestMethod]
        public void Calling_Start_Multiple_Times_Exception()
        {
            using (var test = Create())
            {
                test.Start();
                Assert.ThrowsExactly<DotNetWorkQueueException>(
                    delegate
                    {
                        test.Start();
                    });
            }
        }

        [TestMethod]
        public void Test_Send_Exception()
        {
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();
            var context = Substitute.For<IMessageContext>();
            sendHeartBeat.SendAsync(context).Throws(new ArgumentOutOfRangeException());

            //The interval is long enough that the timer will not fire during the test; the beat is
            //driven directly so the failure path is exercised rather than slept through.
            using (var test = Create(TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5), context, sendHeartBeat))
            {
                test.Start();

                //the worker assigns this in its constructor, so read it back rather than configuring one
                var notification = context.WorkerNotification.HeartBeat;
                _ = test.BeatOnceAsync();

                //the beat is started and left to run, so wait for the failure to be recorded
                var deadline = DateTime.UtcNow.AddSeconds(20);
                while (DateTime.UtcNow < deadline &&
                       notification.ReceivedCalls().All(c => c.GetMethodInfo().Name != nameof(IWorkerHeartBeatNotification.SetError)))
                {
                    Thread.Sleep(50);
                }

                //a beat that throws has to reach user code: the error is recorded and the token tripped,
                //which is how a worker learns its message is no longer protected
                notification.Received(1).SetError(Arg.Any<ArgumentOutOfRangeException>());
            }
        }

        [TestMethod]
        [DataRow(5),
         DataRow(59),
         DataRow(65),
         DataRow(600),
         DataRow(6000),
         DataRow(60000)]
        public void Test_SendDiff(int seconds)
        {
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();
            var context = Substitute.For<IMessageContext>();
            using (var test = Create(TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(2), context, sendHeartBeat))
            {
                test.Start();
                Thread.Sleep(1100);
            }
        }

        [TestMethod]
        public async Task Call_StopAsync_Multiple_Times_Ok()
        {
            using (var test = Create())
            {
                await test.StopAsync();
                await test.StopAsync();
            }
        }

        [TestMethod]
        public async Task DisposeAsync_Sets_IsDisposed()
        {
            var test = Create();
            await test.DisposeAsync();
            Assert.IsTrue(test.IsDisposed);
        }

        [TestMethod]
        public async Task DisposeAsync_Multiple_Times_Ok()
        {
            var test = Create();
            await test.DisposeAsync();
            await test.DisposeAsync();
            Assert.IsTrue(test.IsDisposed);
        }

        [TestMethod]
        public async Task StopAsync_Waits_For_An_InFlight_Beat()
        {
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();
            var context = Substitute.For<IMessageContext>();
            var beatStarted = new ManualResetEventSlim(false);

            //An incomplete task, not a blocking callback: NSubstitute runs the callback inside the call
            //itself, so blocking there would only prove the worker calls SendAsync - not that it awaits
            //what it gets back, which is the whole guarantee under test.
            var beatResult = new TaskCompletionSource<IHeartBeatStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
            sendHeartBeat.SendAsync(context).Returns(_ =>
            {
                beatStarted.Set();
                return beatResult.Task;
            });

            using (var test = Create(TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5), context, sendHeartBeat))
            {
                test.Start();
                _ = Task.Run(() => test.BeatOnceAsync());

                Assert.IsTrue(beatStarted.Wait(TimeSpan.FromSeconds(20)), "the heartbeat never started");

                var stop = test.StopAsync();
                //the beat is still in flight and holding the lock, so the stop cannot have completed
                Assert.IsFalse(stop.IsCompleted, "StopAsync returned while a heartbeat was still updating");

                beatResult.SetResult(Substitute.For<IHeartBeatStatus>());
                await stop;
            }
        }

        [TestMethod]
        public void AClaimThatHasAgedPastTheExpiry_CancelsProcessing()
        {
            //A beat that comes back with no time means the row was not updated - the monitor will reset
            //the message once the stored heartbeat passes the expiry, and hand it to another worker.
            //The worker that is still processing has to be told to stop, or both of them finish it.
            var context = Substitute.For<IMessageContext>();
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();
            var noTime = Substitute.For<IHeartBeatStatus>();
            noTime.LastHeartBeatTime.Returns((DateTime?)null);
            sendHeartBeat.SendAsync(context).Returns(Task.FromResult(noTime));

            var (worker, beat, cancelled) = CreateForStaleness(context, sendHeartBeat, TimeSpan.FromMilliseconds(250));
            using (worker)
            {
                worker.Start();
                beat()();                          //a beat that does not land
                Thread.Sleep(600);                 //age past the expiry

                //keep ticking the way the scheduler does, rather than relying on one tick landing at
                //the right moment - the beat is started and left to run, so a single tick is a race
                var deadline = DateTime.UtcNow.AddSeconds(20);
                while (!cancelled() && DateTime.UtcNow < deadline)
                {
                    beat()();
                    Thread.Sleep(50);
                }

                Assert.IsTrue(cancelled(),
                    "the worker kept processing a message whose claim had lapsed");
            }
        }

        [TestMethod]
        public void AClaimThatIsBeingKeptAlive_DoesNotCancelProcessing()
        {
            //The negative case matters more than the positive one: cancelling a worker whose heartbeat is
            //landing would abort healthy work.
            var context = Substitute.For<IMessageContext>();
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();
            var landed = Substitute.For<IHeartBeatStatus>();
            landed.LastHeartBeatTime.Returns(DateTime.UtcNow);
            sendHeartBeat.SendAsync(context).Returns(Task.FromResult(landed));

            var (worker, beat, cancelled) = CreateForStaleness(context, sendHeartBeat, TimeSpan.FromMilliseconds(250));
            using (worker)
            {
                worker.Start();
                for (var i = 0; i < 5; i++)
                {
                    beat()();
                    Thread.Sleep(100);
                }

                Assert.IsFalse(cancelled(),
                    "a worker whose heartbeat is landing was cancelled anyway");
            }
        }

        [TestMethod]
        public async Task ABeatThatOverrunsTheInterval_DoesNotForfeitTheSchedule()
        {
            //The guarantee #302's catch-up flag used to provide: an overrunning beat must not cost the
            //schedule. Losing slots against a short expiry is how a live worker's claim lapses.
            //What this covers is the resumption - a loop that gave up after the slow beat fails the last
            //assertion. The no-overlap assertion below is held by BeatOnceAsync's own Running guard
            //rather than by the loop, so it is an invariant check, not a test of the timer.
            var context = Substitute.For<IMessageContext>();
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();

            var calls = 0;
            var firstBeat = new ManualResetEventSlim(false);
            var releaseFirst = new TaskCompletionSource<IHeartBeatStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
            sendHeartBeat.SendAsync(context).Returns(_ =>
            {
                //the first beat blocks for far longer than the interval; the rest return at once
                if (Interlocked.Increment(ref calls) == 1)
                {
                    firstBeat.Set();
                    return releaseFirst.Task;
                }
                var status = Substitute.For<IHeartBeatStatus>();
                status.LastHeartBeatTime.Returns(DateTime.UtcNow);
                return Task.FromResult(status);
            });

            using (var test = Create(TimeSpan.FromMinutes(5), TimeSpan.FromMilliseconds(50), context, sendHeartBeat))
            {
                test.Start();
                Assert.IsTrue(firstBeat.Wait(TimeSpan.FromSeconds(20)), "the first heartbeat never started");

                //several intervals pass while the first beat is stuck
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                Assert.AreEqual(1, Interlocked.CompareExchange(ref calls, 0, 0),
                    "a second beat reached the transport while the first was still running");

                releaseFirst.SetResult(Substitute.For<IHeartBeatStatus>());

                //the schedule survived the overrun: beating resumes once the slow beat returns
                var deadline = DateTime.UtcNow.AddSeconds(20);
                while (Interlocked.CompareExchange(ref calls, 0, 0) < 2 && DateTime.UtcNow < deadline)
                {
                    await Task.Delay(25);
                }
                Assert.IsGreaterThanOrEqualTo(2, Interlocked.CompareExchange(ref calls, 0, 0),
                    "beating did not resume after a beat that ran longer than the interval");
            }
        }

        [TestMethod]
        public void ATickThatReadsTheClaimWhileABeatIsLanding_DoesNotCancel()
        {
            //The scheduler starts a tick without waiting for the previous beat, so a beat can have
            //succeeded at the transport and not yet published its timestamp. A tick reading the claim in
            //that window sees the old value; cancelling on it would abort a worker whose claim had just
            //been renewed.
            var context = Substitute.For<IMessageContext>();
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();
            var landed = Substitute.For<IHeartBeatStatus>();
            landed.LastHeartBeatTime.Returns(DateTime.UtcNow);

            var calls = 0;
            var inFlight = new TaskCompletionSource<IHeartBeatStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
            sendHeartBeat.SendAsync(context).Returns(_ =>
                Interlocked.Increment(ref calls) == 1 ? inFlight.Task : Task.FromResult(landed));

            var (worker, beat, cancelled) = CreateForStaleness(context, sendHeartBeat, TimeSpan.FromMilliseconds(600));
            using (worker)
            {
                worker.Start();
                Thread.Sleep(400);          //let the claim age, but not past the expiry

                beat()();                   //a beat starts here, so its timestamp will be recent
                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (Interlocked.CompareExchange(ref calls, 0, 0) < 1 && DateTime.UtcNow < deadline)
                    Thread.Sleep(10);

                Thread.Sleep(300);          //now the claim is past the expiry, measured from the last landed beat
                beat()();                   //this tick sees a lapsed claim while the beat above is still out
                Thread.Sleep(150);

                inFlight.SetResult(landed); //the beat lands, publishing a timestamp from before it started
                Thread.Sleep(600);

                Assert.IsFalse(cancelled(),
                    "a worker was cancelled on a stale reading taken while a heartbeat was landing");
            }
        }

        private (HeartBeatWorker worker,
            Func<Action> beat,
            Func<bool> cancelled)
            CreateForStaleness(IMessageContext context, ISendHeartBeat sendHeartBeat, TimeSpan expiry)
        {
            //the token the worker hands to user code is the one it trips, so capture it on its way through
            var token = CancellationToken.None;
            var factory = Substitute.For<IWorkerHeartBeatNotificationFactory>();
            factory.Create(Arg.Do<CancellationToken>(t => token = t))
                .Returns(Substitute.For<IWorkerHeartBeatNotification>());

            var worker = Create(expiry, TimeSpan.FromMinutes(5), context, sendHeartBeat, factory);
            //read through a closure: the token only exists once the worker has built it
            return (worker, () => () => { _ = worker.BeatOnceAsync(); }, () => token.IsCancellationRequested);
        }

        [TestMethod]
        public async Task ABeatThatNeverReturns_DoesNotBlockTeardownForever()
        {
            //a transport call that hangs holds the beat lock; disposal must give up on it rather than
            //keeping the message from ever completing
            var context = Substitute.For<IMessageContext>();
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();
            var beatStarted = new ManualResetEventSlim(false);
            var never = new TaskCompletionSource<IHeartBeatStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
            sendHeartBeat.SendAsync(context).Returns(_ => { beatStarted.Set(); return never.Task; });

            var test = Create(TimeSpan.FromMinutes(5), TimeSpan.FromMilliseconds(50), context, sendHeartBeat,
                drainTimeout: DrainWindow);
            test.Start();
            Assert.IsTrue(beatStarted.Wait(TimeSpan.FromSeconds(20)), "the heartbeat never started");

            var timer = System.Diagnostics.Stopwatch.StartNew();
            await test.DisposeAsync();
            timer.Stop();

            //under twice the window: the loop wait and the lock wait share one deadline, so a stalled
            //beat costs one drain window rather than one for each
            Assert.IsLessThan(DrainWindow * 1.8, timer.Elapsed,
                "teardown spent more than one drain window on a heartbeat that was never coming back");
            never.SetResult(Substitute.For<IHeartBeatStatus>());
        }

        [TestMethod]
        public void ABeatThatNeverReturns_DoesNotBlockSynchronousTeardownForever()
        {
            var context = Substitute.For<IMessageContext>();
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();
            var beatStarted = new ManualResetEventSlim(false);
            var never = new TaskCompletionSource<IHeartBeatStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
            sendHeartBeat.SendAsync(context).Returns(_ => { beatStarted.Set(); return never.Task; });

            var test = Create(TimeSpan.FromMinutes(5), TimeSpan.FromMilliseconds(50), context, sendHeartBeat,
                drainTimeout: DrainWindow);
            test.Start();
            Assert.IsTrue(beatStarted.Wait(TimeSpan.FromSeconds(20)), "the heartbeat never started");

            var timer = System.Diagnostics.Stopwatch.StartNew();
            test.Dispose();
            timer.Stop();

            //under twice the window: the loop wait and the lock wait share one deadline, so a stalled
            //beat costs one drain window rather than one for each
            Assert.IsLessThan(DrainWindow * 1.8, timer.Elapsed,
                "teardown spent more than one drain window on a heartbeat that was never coming back");
            never.SetResult(Substitute.For<IHeartBeatStatus>());
        }

        [TestMethod]
        public async Task StartingAndStoppingWithoutABeat_TearsDownCleanly()
        {
            //the interval never elapses, so the loop is parked on the timer the whole time
            var context = Substitute.For<IMessageContext>();
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();

            var test = Create(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5), context, sendHeartBeat);
            test.Start();
            await test.StopAsync();
            await test.DisposeAsync();

            await sendHeartBeat.DidNotReceiveWithAnyArgs().SendAsync(null);
        }

        [TestMethod]
        public async Task ABeatAfterStop_DoesNotReachTheTransport()
        {
            //a stopped worker has given up its claim; beating would renew it and put the worker back to
            //looking alive while it is shutting down
            var context = Substitute.For<IMessageContext>();
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();

            using (var test = Create(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5), context, sendHeartBeat))
            {
                test.Start();
                await test.StopAsync();

                await test.BeatOnceAsync();

                await sendHeartBeat.DidNotReceiveWithAnyArgs().SendAsync(null);
            }
        }

        [TestMethod]
        public async Task ASecondBeatWhileOneIsRunning_IsSkipped()
        {
            //the loop cannot produce this - it does not ask for the next tick until the beat returns -
            //but the guard is what keeps a direct caller from overlapping two updates on one message
            var context = Substitute.For<IMessageContext>();
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();
            var beatStarted = new ManualResetEventSlim(false);
            var release = new TaskCompletionSource<IHeartBeatStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
            sendHeartBeat.SendAsync(context).Returns(_ => { beatStarted.Set(); return release.Task; });

            using (var test = Create(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5), context, sendHeartBeat))
            {
                test.Start();
                var first = test.BeatOnceAsync();
                Assert.IsTrue(beatStarted.Wait(TimeSpan.FromSeconds(20)), "the first heartbeat never started");

                await test.BeatOnceAsync();

                await sendHeartBeat.Received(1).SendAsync(context);

                release.SetResult(Substitute.For<IHeartBeatStatus>());
                await first;
            }
        }

        private HeartBeatWorker Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return Create(TimeSpan.Zero, TimeSpan.FromSeconds(59), fixture.Create<IMessageContext>(), fixture.Create<ISendHeartBeat>());
        }


        private HeartBeatWorker Create(TimeSpan checkSpan, TimeSpan updateTime, IMessageContext context, ISendHeartBeat sendHeartBeat,
            IWorkerHeartBeatNotificationFactory notificationFactory = null, TimeSpan drainTimeout = default)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(context);
            fixture.Inject(sendHeartBeat);

            //a logger that reports every level as enabled. Without this the trace and debug branches
            //never run, and one of them reads status.MessageId.Id.Value - the kind of line that throws
            //only once it is actually reached
            var logger = Substitute.For<ILogger>();
            logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
            fixture.Inject(logger);
            if (notificationFactory != null)
                fixture.Inject(notificationFactory);

            //the worker reads the transport's clock, not the machine's - a substitute would hand it
            //DateTime.MinValue and nothing would ever look stale
            var getTime = Substitute.For<IGetTime>();
            getTime.GetCurrentUtcDate().Returns(_ => DateTime.UtcNow);
            var getTimeFactory = Substitute.For<IGetTimeFactory>();
            getTimeFactory.Create().Returns(getTime);
            fixture.Inject(getTimeFactory);
            var threadPoolConfiguration = fixture.Create<IHeartBeatThreadPoolConfiguration>();
            threadPoolConfiguration.ThreadsMax.Returns(1);
            threadPoolConfiguration.WaitForThreadPoolToFinish
                .Returns(drainTimeout == default ? TimeSpan.FromSeconds(5) : drainTimeout);
            fixture.Inject(threadPoolConfiguration);
            IHeartBeatConfiguration configuration = fixture.Create<HeartBeatConfiguration>();
            configuration.Time = checkSpan;
            configuration.UpdateTime = updateTime;
            fixture.Inject(configuration);
            //a gate that hands out a slot straight away; the bound itself has its own tests
            var gate = Substitute.For<IHeartBeatGate>();
            gate.EnterAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
                .Returns(_ => Task.FromResult<IDisposable>(new NoOpSlot()));
            fixture.Inject(gate);
            return fixture.Create<HeartBeatWorker>();
        }

        private sealed class NoOpSlot : IDisposable
        {
            public void Dispose() { }
        }
    }
}
