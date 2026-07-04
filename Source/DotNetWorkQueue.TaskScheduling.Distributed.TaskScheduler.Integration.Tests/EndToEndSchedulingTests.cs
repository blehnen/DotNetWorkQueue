using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests
{
    /// <summary>
    /// Smoke test: proves the shipped 0.4.0 NuGet package integrates cleanly into a
    /// real DNQ consumer's SimpleInjector container via InjectDistributedTaskScheduler.
    /// SimpleInjector runs full Verify() during QueueContainer construction, so any
    /// missing binding introduced by the scheduler injection would throw here.
    ///
    /// NOTE — scope reduction from original PLAN-2.1 (documented in SUMMARY-2.1.md):
    /// The original plan called for producing N messages and consuming them via the
    /// scheduler-wired container, but DNQ's shared ConsumerSharedRunner exposes
    /// Action&lt;TTransportCreate&gt; for transport options — not Action&lt;IContainer&gt; for
    /// container registration — so there is no seam through which a hand-rolled test
    /// can both reuse the shared producer/consumer pattern AND inject the scheduler.
    /// The Memory transport's per-container in-process storage blocks a naive two-
    /// container hand-roll (producer and consumer see different stores). Phase 3's
    /// critical cross-repo regression guard is ConcurrencyRegressionTests which
    /// exercises the real scheduler injection path end-to-end via IContainer; this
    /// smoke test closes the remaining gap by proving SimpleInjector verification
    /// accepts the injection without errors.
    /// </summary>
    [TestClass]
    public class EndToEndSchedulingTests
    {
        private static int _portCounter = TestHelpers.EndToEndPortBase;

        [TestMethod]
        public void InjectDistributedTaskScheduler_WiresIntoMemoryConsumerContainer_VerifyPasses()
        {
            var port = TestHelpers.NextPort(ref _portCounter);
            // DNQ queue names must be alphanumeric/underscore/dot — "N" format drops hyphens.
            var queueName = "q" + Guid.NewGuid().ToString("N");
            var queueConnection = new QueueConnection(queueName, "none");
            var logProvider = LoggerShared.Create(queueName, GetType().Name);

            using (var queueCreator = new QueueCreationContainer<MemoryMessageQueueInit>(
                sr => sr.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var oCreation = queueCreator.GetQueueCreation<MessageQueueCreation>(queueConnection);
                try
                {
                    var result = oCreation.CreateQueue();
                    Assert.IsTrue(result.Success, result.ErrorMessage);

                    // The real assertion — constructing the consumer container runs
                    // SimpleInjector Verify(), which builds every registered binding.
                    // Any mis-wired dependency introduced by InjectDistributedTaskScheduler
                    // throws an ActivationException here.
                    // ISSUE-030: positional args only (not `udpBroadcastPort:`).
                    using (var creator = new QueueContainer<MemoryMessageQueueInit>(
                        sr => sr
                            .Register(() => logProvider, LifeStyles.Singleton)
                            .RegisterNonScopedSingleton(oCreation.Scope)
                            .InjectDistributedTaskScheduler(port, TestHelpers.BeaconInterface)))
                    {
                        using (var queue = creator.CreateConsumer(queueConnection))
                        {
                            Assert.IsNotNull(queue,
                                "scheduler-wired consumer container constructed and resolved a consumer");
                            Assert.IsNotNull(queue.Configuration,
                                "the scheduler injection must not break configuration resolution");
                        }
                    }
                }
                finally
                {
                    oCreation?.RemoveQueue();
                    oCreation?.Dispose();
                }
            }
        }
    }
}
