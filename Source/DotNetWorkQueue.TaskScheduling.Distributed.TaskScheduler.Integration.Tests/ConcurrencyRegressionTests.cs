using System;
using System.Threading.Tasks;
using DotNetWorkQueue;
using DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests
{
    /// <summary>
    /// Cross-repo regression guard for Phase 1's TaskSchedulerJobCountSync lock fix.
    /// Hammers IncreaseCurrentTaskCount/DecreaseCurrentTaskCount from many threads to
    /// detect deadlock and assert final count consistency without relying on real DNQ jobs.
    /// </summary>
    [TestClass]
    public class ConcurrencyRegressionTests
    {
        private static int _portCounter = TestHelpers.ConcurrencyPortBase;

        private SchedulerContainer _schedulerContainer;
        private ITaskSchedulerJobCountSync _sync;

        [TestCleanup]
        public void Cleanup()
        {
            _sync?.Dispose();
            _sync = null;
            _schedulerContainer?.Dispose();
            _schedulerContainer = null;
        }

        [TestMethod]
        public void HammerIncreaseDecrease_NoDeadlock_FinalCountConsistent()
        {
            var port = TestHelpers.NextPort(ref _portCounter);

            // Capture the IContainer from the registerService callback so we can
            // resolve ITaskSchedulerJobCountSync after InjectDistributedTaskScheduler registers it.
            IContainer capturedContainer = null;
            _schedulerContainer = new SchedulerContainer(container =>
            {
                capturedContainer = container;
                container.InjectDistributedTaskScheduler(port, TestHelpers.BeaconInterface);
            });

            // CreateTaskScheduler() triggers the container build, which invokes the
            // registerService callback and populates capturedContainer.
            _schedulerContainer.CreateTaskScheduler();

            _sync = capturedContainer.GetInstance<ITaskSchedulerJobCountSync>();

            // CRITICAL: call Start() before spawning threads so _outbound is initialized
            // and the real concurrency path (not the null-safe no-op guard) is exercised.
            // A test that skips Start() is a false positive — it would pass even if Phase 1's
            // lock fix were reverted.
            _sync.Start();

            const int Threads = 12;
            const int Iterations = 5000;

            var tasks = new Task[Threads];
            for (int t = 0; t < Threads; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < Iterations; i++)
                    {
                        _sync.IncreaseCurrentTaskCount();
                        _sync.DecreaseCurrentTaskCount();
                    }
                });
            }

            bool completed = Task.WaitAll(tasks, TimeSpan.FromSeconds(30));
            if (!completed)
            {
                Assert.Fail("Deadlock detected: 30-second timeout elapsed waiting for worker threads");
            }

            Assert.AreEqual(0, _sync.GetCurrentTaskCount(), "all increments are matched by decrements; final count must be zero");
        }
    }
}
