// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Concurrency
{
    /// <summary>
    /// Red-gate baseline test for the SE.Redis 3.x thread-pool starvation bug (#161).
    ///
    /// PHASE 1 PURPOSE: This test is EXPECTED TO FAIL with "Timeout performing EVAL"
    /// on the unfixed synchronous ReceiveMessageQueryHandler / DequeueLua.Execute path.
    /// Phase 1 success = this test reliably FAILS. Phases 2-3 turn it green.
    ///
    /// DETERMINISTIC APPROACH (CONTEXT-1 D4): Rather than relying on natural load to
    /// accidentally starve the pool (which may not reproduce reliably on fast local Redis),
    /// this test explicitly caps the global thread-pool worker count to a small value (N),
    /// then floods MORE than N concurrent synchronous EVAL operations — guaranteeing that
    /// all capped worker threads block on ScriptEvaluate() with no thread left to deliver
    /// the already-arrived Redis reply, producing a deterministic EVAL timeout.
    ///
    /// Thread pool is CAPPED (not raised): setting both min and max WORKER threads to N
    /// exposes the bug. The original CONTEXT-1 guardrail "no SetMinThreads" meant do not
    /// RAISE min to mask starvation; capping DOWN to expose it is the opposite action
    /// and is exactly the point of this baseline (CONTEXT-1 D4).
    ///
    /// Original values are ALWAYS RESTORED in a finally block — the setting is process-global.
    ///
    /// Excluded from default suite via: --filter "TestCategory!=StarvationBaseline"
    /// Run in isolation via:          --filter "TestCategory=StarvationBaseline"
    /// </summary>
    [TestClass]
    public class StarvationBaselineTests
    {
        // Cap worker threads to this value. Must be low enough that the concurrent EVALs
        // saturate all available workers, but high enough that the test harness itself
        // can acquire a thread (avoids deadlocking the test runner before Redis is even
        // reached). Tuned at 6: concurrent senders = 50, well above the cap.
        private const int WorkerCap = 6;

        // Number of concurrent parallel sender tasks driving sync EVAL calls.
        // Must exceed WorkerCap so all capped workers are blocked on ScriptEvaluate()
        // with no thread free to deliver the reply completion.
        private const int ConcurrentSenders = 50;

        // Messages per sender task. Kept low (10) — we want rapid concurrent saturation,
        // not a slow sequential drain. Total enqueued = ConcurrentSenders * MessagesPerSender.
        private const int MessagesPerSender = 10;

        /// <summary>
        /// Deterministic thread-pool starvation baseline.
        ///
        /// EXPECTED OUTCOME (Phase 1): FAILED — "Timeout performing EVAL (7000ms)" from
        /// StackExchange.Redis.RedisTimeoutException surfacing through the sync
        /// BaseLua.TryExecute -> ScriptEvaluate -> ReceiveMessageQueryHandler.Handle chain.
        ///
        /// DO NOT attempt to fix this failure — it is the deliverable for Phase 1.
        /// Phases 2-3 make it green by switching to the async path.
        /// </summary>
        [TestMethod]
        [TestCategory("StarvationBaseline")]
        public void StarvationBaseline_CappedPool_SyncEvalFlood_FailsWithTimeout()
        {
            var connectionString = ConnectionInfo.ConnectionString;
            var queueConnection = new QueueConnection(GenerateQueueName.Create(), connectionString);

            // --- Save original thread-pool settings (process-global; MUST be restored) ---
            ThreadPool.GetMinThreads(out var origMinWorker, out var origMinIocp);
            ThreadPool.GetMaxThreads(out var origMaxWorker, out var origMaxIocp);

            try
            {
                // --- Cap the worker thread pool ---
                // Order: lower MIN first (max >= min is required), then lower MAX.
                // Leave IOCP (completion-port) threads at original values.
                ThreadPool.SetMinThreads(WorkerCap, origMinIocp);
                ThreadPool.SetMaxThreads(WorkerCap, origMaxIocp);

                // --- Create the queue ---
                using var queueCreator = new QueueCreationContainer<RedisQueueInit>();
                using var creation = queueCreator.GetQueueCreation<RedisQueueCreation>(queueConnection);
                var result = creation.CreateQueue();
                Assert.IsTrue(result.Success, result.ErrorMessage);

                Exception caughtException = null;

                try
                {
                    // --- Produce messages with concurrent parallel senders ---
                    // Each sender task calls queue.Send() which issues a sync ScriptEvaluate
                    // (EVAL) on the Redis transport. With WorkerCap worker threads and
                    // ConcurrentSenders parallel tasks, all threads park on ScriptEvaluate()
                    // with none left to deliver reply completions — triggering the timeout.
                    using var container = new QueueContainer<RedisQueueInit>();
                    using var producer = container.CreateProducer<FakeMessage>(queueConnection);

                    var senderTasks = Enumerable.Range(0, ConcurrentSenders).Select(_ => new Task(() =>
                    {
                        for (var i = 0; i < MessagesPerSender; i++)
                        {
                            var msg = new FakeMessage { Name = Guid.NewGuid().ToString() };
                            producer.Send(msg);
                        }
                    })).ToList();

                    // Launch all senders simultaneously to maximise concurrent EVAL pressure
                    foreach (var t in senderTasks) t.Start();
                    Task.WaitAll(senderTasks.ToArray());
                }
                catch (AggregateException agg)
                {
                    // Flatten and capture the first Redis timeout exception
                    caughtException = agg.Flatten().InnerExceptions
                        .FirstOrDefault(e => e is RedisTimeoutException ||
                                             (e.InnerException is RedisTimeoutException) ||
                                             e.Message.Contains("Timeout performing EVAL") ||
                                             e.Message.Contains("Timeout performing SCRIPT")) ?? agg;
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
                finally
                {
                    // Best-effort queue cleanup (may also timeout under capped pool;
                    // suppress — the test result is already captured above)
                    try { creation.RemoveQueue(); } catch { /* intentional */ }
                }

                // --- Assertion: expect NO timeout (so the test FAILS red on unfixed path) ---
                // When the EVAL timeout fires, caughtException is non-null and the
                // Assert below throws, surfacing the Redis timeout as the test failure.
                Assert.IsNull(caughtException,
                    $"Thread-pool starvation reproduced — SE.Redis 3.x sync EVAL timed out. " +
                    $"This is the expected Phase-1 red baseline. Fix arrives in Phases 2-3.\n" +
                    $"Exception: {caughtException?.Message}");
            }
            finally
            {
                // --- ALWAYS restore original thread-pool settings ---
                // Order matters: max must be restored BEFORE min. At this point the pool is
                // capped at min=max=WorkerCap; calling SetMinThreads(origMin) first would fail
                // silently (returns false) because origMin > current max=WorkerCap, leaving the
                // process pinned at the low worker count and corrupting every later test in the
                // same process. Raise max back first, then restore min.
                ThreadPool.SetMaxThreads(origMaxWorker, origMaxIocp);
                ThreadPool.SetMinThreads(origMinWorker, origMinIocp);
            }
        }
    }
}
