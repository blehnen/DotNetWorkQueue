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

        private HeartBeatWorker Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return Create(TimeSpan.Zero, "*/59 * * * * *", fixture.Create<IMessageContext>(), fixture.Create<ISendHeartBeat>());
        }


        private HeartBeatWorker Create(TimeSpan checkSpan, string updateTime, IMessageContext context, ISendHeartBeat sendHeartBeat,
            IHeartBeatScheduler scheduler = null)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(context);
            fixture.Inject(sendHeartBeat);
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
