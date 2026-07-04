using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue;
using DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests
{
    /// <summary>
    /// Verifies the NetMQ beacon-based node discovery protocol: two SchedulerContainers
    /// sharing the same UDP broadcast port must see each other, converge on a common
    /// task-count view, and the surviving node must observe the other node decaying
    /// after disposal.
    /// </summary>
    [TestClass]
    public class NodeDiscoveryTests
    {
        private static int _portCounter = TestHelpers.NodeDiscoveryPortBase;

        private sealed class Node : IDisposable
        {
            public SchedulerContainer SchedulerContainer { get; }
            public ITaskSchedulerJobCountSync Sync { get; }

            private Node(SchedulerContainer container, ITaskSchedulerJobCountSync sync)
            {
                SchedulerContainer = container;
                Sync = sync;
            }

            public static Node Create(int port)
            {
                IContainer capturedContainer = null;
                var schedulerContainer = new SchedulerContainer(c =>
                {
                    capturedContainer = c;
                    c.InjectDistributedTaskScheduler(port, TestHelpers.BeaconInterface);
                });

                // CreateTaskScheduler() triggers the container build, which invokes the
                // registerService callback and populates capturedContainer. Mirrors the
                // closure pattern used in ConcurrencyRegressionTests.cs.
                schedulerContainer.CreateTaskScheduler();

                var sync = capturedContainer.GetInstance<ITaskSchedulerJobCountSync>();
                return new Node(schedulerContainer, sync);
            }

            public void Dispose()
            {
                Sync?.Dispose();
                SchedulerContainer?.Dispose();
            }
        }

        [TestMethod]
        public void TwoNodes_SharedPort_DiscoverEachOther_RemoteCountConverges()
        {
            var sharedPort = TestHelpers.NextPort(ref _portCounter);

            using (var nodeA = Node.Create(sharedPort))
            using (var nodeB = Node.Create(sharedPort))
            {
                // Start() must be called before any wire messages flow so _outbound is
                // initialized and the UDP beacon loop is active.
                nodeA.Sync.Start();
                nodeB.Sync.Start();

                // Subscribe to RemoteCountChanged on node B before bumping A so we don't
                // miss the transition.
                var signal = new ManualResetEventSlim(false);
                nodeB.Sync.RemoteCountChanged += (s, e) => signal.Set();

                // Bump node A's local count — this should propagate to node B via UDP beacon.
                nodeA.Sync.IncreaseCurrentTaskCount();

                // Wait up to 10 seconds for node B to receive the update (UDP + timer-driven).
                var fired = signal.Wait(TimeSpan.FromSeconds(10));
                Assert.IsTrue(fired, "node B must receive RemoteCountChanged from node A within 10 seconds");

                // Node B's aggregate count should reflect node A's bump.
                Assert.IsTrue(nodeB.Sync.GetCurrentTaskCount() >= 1,
                    "node B should see node A's remote task count");
            }
        }

        [TestMethod]
        public void NodeStop_RemoteCountDecays()
        {
            var sharedPort = TestHelpers.NextPort(ref _portCounter);

            var nodeA = Node.Create(sharedPort);
            using (var nodeB = Node.Create(sharedPort))
            {
                try
                {
                    nodeA.Sync.Start();
                    nodeB.Sync.Start();

                    // Wait for initial handshake so both nodes have seen each other, then
                    // bump node A so node B has a non-zero remote view to "forget".
                    var discoverySignal = new ManualResetEventSlim(false);
                    nodeB.Sync.RemoteCountChanged += (s, e) => discoverySignal.Set();

                    nodeA.Sync.IncreaseCurrentTaskCount();

                    var discovered = discoverySignal.Wait(TimeSpan.FromSeconds(10));
                    Assert.IsTrue(discovered, "node B must see node A's count before we test disposal");

                    var countBeforeDispose = nodeB.Sync.GetCurrentTaskCount();
                    Assert.IsTrue(countBeforeDispose >= 1, "should have a remote count to decay");

                    // Dispose node A — node B should observe it drop off the remote list.
                    nodeA.Dispose();

                    // Poll node B's count until it decays (or timeout after 15 seconds).
                    var deadline = DateTime.UtcNow.AddSeconds(15);
                    long countAfterDispose = countBeforeDispose;
                    while (DateTime.UtcNow < deadline)
                    {
                        countAfterDispose = nodeB.Sync.GetCurrentTaskCount();
                        if (countAfterDispose < countBeforeDispose)
                            break;
                        Thread.Sleep(100);
                    }

                    Assert.IsTrue(countAfterDispose < countBeforeDispose,
                        "node B's aggregate count should drop after node A is disposed (RemovedNode decay)");
                }
                finally
                {
                    // In case nodeA.Dispose() wasn't reached.
                    nodeA.Dispose();
                }
            }
        }
    }
}
