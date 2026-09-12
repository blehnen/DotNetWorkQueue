using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Queue;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.Queue
{
    /// <summary>
    /// The bound on how many heartbeat updates a consumer has in flight.
    ///
    /// The rule that matters is that a beat which cannot get a slot waits rather than being dropped.
    /// Dropping a beat is what costs a message its claim, which is the defect this gate sits next to.
    /// </summary>
    [TestClass]
    public class HeartBeatGateTests
    {
        [TestMethod]
        public async Task ThreadsMax_BoundsBeatsInFlight()
        {
            using (var gate = Create(threadsMax: 2, workerCount: 10))
            {
                var first = await gate.EnterAsync(TimeSpan.FromSeconds(30), CancellationToken.None);
                var second = await gate.EnterAsync(TimeSpan.FromSeconds(30), CancellationToken.None);

                var third = gate.EnterAsync(TimeSpan.FromSeconds(30), CancellationToken.None);
                Assert.IsFalse(third.IsCompleted, "a third beat got a slot when only two were allowed");

                first.Dispose();
                var granted = await Task.WhenAny(third, Task.Delay(TimeSpan.FromSeconds(20)));
                Assert.AreSame(third, granted, "the waiting beat was not given the released slot");

                (await third).Dispose();
                second.Dispose();
            }
        }

        [TestMethod]
        public async Task AnUnsetThreadsMax_AllowsOneBeatPerWorker()
        {
            //the natural ceiling: there is at most one beat per in-flight message
            using (var gate = Create(threadsMax: 0, workerCount: 3))
            {
                var held = new IDisposable[3];
                for (var i = 0; i < 3; i++)
                    held[i] = await gate.EnterAsync(TimeSpan.FromSeconds(30), CancellationToken.None);

                var fourth = gate.EnterAsync(TimeSpan.FromSeconds(30), CancellationToken.None);
                Assert.IsFalse(fourth.IsCompleted, "more beats were allowed than there are workers");

                held[0].Dispose();
                (await fourth).Dispose();
                held[1].Dispose();
                held[2].Dispose();
            }
        }

        [TestMethod]
        public async Task ASlotIsReleasedOnlyOnce_EvenIfDisposedTwice()
        {
            using (var gate = Create(threadsMax: 1, workerCount: 1))
            {
                var slot = await gate.EnterAsync(TimeSpan.FromSeconds(30), CancellationToken.None);
                slot.Dispose();
                slot.Dispose();

                //a double release would hand out two slots where there is one
                var again = await gate.EnterAsync(TimeSpan.FromSeconds(30), CancellationToken.None);
                var extra = gate.EnterAsync(TimeSpan.FromSeconds(30), CancellationToken.None);
                Assert.IsFalse(extra.IsCompleted, "disposing a slot twice released it twice");
                again.Dispose();
                (await extra).Dispose();
            }
        }

        [TestMethod]
        public async Task ABeatThatCannotGetASlotInTime_GoesAheadWithoutOne()
        {
            //the bound is a throttle, not a gate on correctness: a beat held back until its claim has
            //lapsed would make the worker cancel itself over a queue this class created
            using (var gate = Create(threadsMax: 1, workerCount: 1))
            {
                var held = await gate.EnterAsync(TimeSpan.FromSeconds(30), CancellationToken.None);

                var overflow = await gate.EnterAsync(TimeSpan.FromMilliseconds(50), CancellationToken.None);
                Assert.IsNotNull(overflow, "a beat that timed out waiting was not allowed to proceed");

                //disposing the one it never held must not hand back a slot it does not own
                overflow.Dispose();
                held.Dispose();

                var first = await gate.EnterAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
                var second = gate.EnterAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
                Assert.IsFalse(second.IsCompleted, "the timed-out beat released a slot it never took");
                first.Dispose();
                (await second).Dispose();
            }
        }

        [TestMethod]
        public async Task ACancelledWait_Throws()
        {
            using (var gate = Create(threadsMax: 1, workerCount: 1))
            using (var cancel = new CancellationTokenSource())
            {
                var held = await gate.EnterAsync(TimeSpan.FromSeconds(30), CancellationToken.None);
                var waiting = gate.EnterAsync(TimeSpan.FromSeconds(30), cancel.Token);
                cancel.Cancel();
                await Assert.ThrowsAsync<OperationCanceledException>(async () => await waiting);
                held.Dispose();
            }
        }

        [TestMethod]
        public async Task ASlotTakenAfterDisposal_DoesNotThrow()
        {
            //teardown can race a beat on its way out; releasing into a disposed gate is not an error
            var gate = Create(threadsMax: 1, workerCount: 1);
            var slot = await gate.EnterAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            gate.Dispose();
            slot.Dispose();
        }

        [TestMethod]
        public async Task ASlotThatTookLongerThanTheInterval_IsReported()
        {
            //the condition that used to be invisible: under the old scheduler a beat that could not get
            //a thread simply sat in a queue and nothing said so
            var logger = Substitute.For<ILogger>();
            logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

            using (var gate = Create(threadsMax: 1, workerCount: 1,
                       updateTime: TimeSpan.FromMilliseconds(10), logger: logger))
            {
                var held = await gate.EnterAsync(TimeSpan.FromSeconds(30), CancellationToken.None);

                //every acquisition is measured, including that one - a pause taking it over the 10ms
                //interval would log a warning that has nothing to do with what this test is about
                logger.ClearReceivedCalls();

                var queued = gate.EnterAsync(TimeSpan.FromSeconds(30), CancellationToken.None);
                Assert.IsFalse(queued.IsCompleted, "the second beat should be waiting for the only slot");

                await Task.Delay(120);          //longer than the 10ms interval
                held.Dispose();
                (await queued).Dispose();

                logger.ReceivedWithAnyArgs().Log(LogLevel.Warning, default, default(object), null, default!);
            }
        }

        private static HeartBeatGate Create(int threadsMax, int workerCount)
            => Create(threadsMax, workerCount, TimeSpan.FromMinutes(5), Substitute.For<ILogger>());

        private static HeartBeatGate Create(int threadsMax, int workerCount, TimeSpan updateTime, ILogger logger)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            var threadPool = Substitute.For<IHeartBeatThreadPoolConfiguration>();
            threadPool.ThreadsMax.Returns(threadsMax);
            fixture.Inject(threadPool);

            var heartBeat = Substitute.For<IHeartBeatConfiguration>();
            heartBeat.ThreadPoolConfiguration.Returns(threadPool);
            heartBeat.UpdateTime.Returns(updateTime);
            fixture.Inject(heartBeat);

            var worker = Substitute.For<IWorkerConfiguration>();
            worker.WorkerCount.Returns(workerCount);
            fixture.Inject(worker);

            return new HeartBeatGate(fixture.Create<QueueConsumerConfiguration>(), logger);
        }
    }
}
