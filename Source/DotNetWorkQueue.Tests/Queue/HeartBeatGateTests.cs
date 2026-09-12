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
                var first = await gate.EnterAsync(CancellationToken.None);
                var second = await gate.EnterAsync(CancellationToken.None);

                var third = gate.EnterAsync(CancellationToken.None);
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
                    held[i] = await gate.EnterAsync(CancellationToken.None);

                var fourth = gate.EnterAsync(CancellationToken.None);
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
                var slot = await gate.EnterAsync(CancellationToken.None);
                slot.Dispose();
                slot.Dispose();

                //a double release would hand out two slots where there is one
                var again = await gate.EnterAsync(CancellationToken.None);
                var extra = gate.EnterAsync(CancellationToken.None);
                Assert.IsFalse(extra.IsCompleted, "disposing a slot twice released it twice");
                again.Dispose();
                (await extra).Dispose();
            }
        }

        private static HeartBeatGate Create(int threadsMax, int workerCount)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            var threadPool = Substitute.For<IHeartBeatThreadPoolConfiguration>();
            threadPool.ThreadsMax.Returns(threadsMax);
            fixture.Inject(threadPool);

            var heartBeat = Substitute.For<IHeartBeatConfiguration>();
            heartBeat.ThreadPoolConfiguration.Returns(threadPool);
            heartBeat.UpdateTime.Returns(TimeSpan.FromMinutes(5));
            fixture.Inject(heartBeat);

            var worker = Substitute.For<IWorkerConfiguration>();
            worker.WorkerCount.Returns(workerCount);
            fixture.Inject(worker);

            return new HeartBeatGate(fixture.Create<QueueConsumerConfiguration>(), Substitute.For<ILogger>());
        }
    }
}
