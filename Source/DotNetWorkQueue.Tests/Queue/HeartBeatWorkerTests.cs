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
using NSubstitute;
using NSubstitute.ExceptionExtensions;


using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class HeartBeatWorkerTests
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

            //The scheduler is a substitute, so nothing fires the job on its own. Sleeping and hoping -
            //which this test used to do - exercised none of the failure path; run what the worker
            //scheduled instead.
            Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> scheduled = null;
            var scheduler = Substitute.For<IHeartBeatScheduler>();
            scheduler.AddUpdateJob(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Do<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>(x => scheduled = x));

            using (var test = Create(TimeSpan.FromSeconds(5), "*/2 * * * * *", context, sendHeartBeat, scheduler))
            {
                test.Start();
                Assert.IsNotNull(scheduled, "the worker did not schedule a heartbeat");

                //the worker assigns this in its constructor, so read it back rather than configuring one
                var notification = context.WorkerNotification.HeartBeat;
                scheduled.Compile()(null, null);

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
            using (var test = Create(TimeSpan.FromSeconds(seconds), "*/2 * * * * *", context, sendHeartBeat))
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

            //the scheduler is a substitute, so nothing fires the job on its own - capture what the
            //worker scheduled and run it directly
            Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> scheduled = null;
            var scheduler = Substitute.For<IHeartBeatScheduler>();
            scheduler.AddUpdateJob(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Do<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>(x => scheduled = x));

            using (var test = Create(TimeSpan.FromSeconds(5), "*/1 * * * * *", context, sendHeartBeat, scheduler))
            {
                test.Start();
                Assert.IsNotNull(scheduled, "the worker did not schedule a heartbeat");
                _ = Task.Run(() => scheduled.Compile()(null, null));

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
        public void ATickThatArrivesDuringABeat_IsTakenWhenThatBeatFinishes()
        {
            //A tick that lands while a beat is still running used to be dropped, and the next chance was
            //a whole schedule away. With a 3 second schedule against a 10 second expiry that is a third
            //of the window per lost tick, which is how a claim lapses under a worker that is alive.
            var context = Substitute.For<IMessageContext>();
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();
            var landed = Substitute.For<IHeartBeatStatus>();
            landed.LastHeartBeatTime.Returns(DateTime.UtcNow);

            var calls = 0;
            var inFlight = new TaskCompletionSource<IHeartBeatStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
            sendHeartBeat.SendAsync(context).Returns(_ =>
                Interlocked.Increment(ref calls) == 1 ? inFlight.Task : Task.FromResult(landed));

            var (worker, beat, _) = CreateForStaleness(context, sendHeartBeat, TimeSpan.FromSeconds(30));
            using (worker)
            {
                worker.Start();

                beat()();  //starts a beat that will not come back yet
                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (Interlocked.CompareExchange(ref calls, 0, 0) < 1 && DateTime.UtcNow < deadline)
                    Thread.Sleep(10);
                Assert.AreEqual(1, Interlocked.CompareExchange(ref calls, 0, 0), "the first beat never started");

                beat()();  //this tick finds the first beat still running

                inFlight.SetResult(landed);

                //the skipped tick has to turn into a beat now, not at the next schedule
                deadline = DateTime.UtcNow.AddSeconds(10);
                while (Interlocked.CompareExchange(ref calls, 0, 0) < 2 && DateTime.UtcNow < deadline)
                    Thread.Sleep(10);

                Assert.IsGreaterThanOrEqualTo(2, Interlocked.CompareExchange(ref calls, 0, 0),
                    "the tick that arrived during a beat was dropped instead of being taken afterwards");
            }
        }

        private (HeartBeatWorker worker,
            Func<Action> beat,
            Func<bool> cancelled)
            CreateForStaleness(IMessageContext context, ISendHeartBeat sendHeartBeat, TimeSpan expiry)
        {
            Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> scheduled = null;
            var scheduler = Substitute.For<IHeartBeatScheduler>();
            scheduler.AddUpdateJob(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Do<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>(x => scheduled = x));

            //the token the worker hands to user code is the one it trips, so capture it on its way through
            var token = CancellationToken.None;
            var factory = Substitute.For<IWorkerHeartBeatNotificationFactory>();
            factory.Create(Arg.Do<CancellationToken>(t => token = t))
                .Returns(Substitute.For<IWorkerHeartBeatNotification>());

            var worker = Create(expiry, "*/1 * * * * *", context, sendHeartBeat, scheduler, factory);
            //read through closures: the job is only scheduled once Start runs, and the token only exists
            //once the worker has built it
            return (worker, () => () => scheduled.Compile()(null, null), () => token.IsCancellationRequested);
        }

        private HeartBeatWorker Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return Create(TimeSpan.Zero, "*/59 * * * * *", fixture.Create<IMessageContext>(), fixture.Create<ISendHeartBeat>());
        }


        private HeartBeatWorker Create(TimeSpan checkSpan, string updateTime, IMessageContext context, ISendHeartBeat sendHeartBeat,
            IHeartBeatScheduler scheduler = null, IWorkerHeartBeatNotificationFactory notificationFactory = null)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(context);
            fixture.Inject(sendHeartBeat);
            if (notificationFactory != null)
                fixture.Inject(notificationFactory);
            var threadPoolConfiguration = fixture.Create<IHeartBeatThreadPoolConfiguration>();
            threadPoolConfiguration.ThreadsMax.Returns(1);
            fixture.Inject(threadPoolConfiguration);
            IHeartBeatConfiguration configuration = fixture.Create<HeartBeatConfiguration>();
            configuration.Time = checkSpan;
            configuration.UpdateTime = updateTime;
            fixture.Inject(configuration);
            var threadpool = scheduler ?? fixture.Create<IHeartBeatScheduler>();
            fixture.Inject(threadpool);
            return fixture.Create<HeartBeatWorker>();
        }
    }
}
