using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Concurrency
{
    /// <summary>
    /// Thread-pool starvation on the RECEIVE path — the gate for #256, plus the control that
    /// establishes what does and does not cause it.
    ///
    /// <see cref="StarvationBaselineTests"/> looks like it already covers this and does not. It
    /// floods with fifty concurrent <c>producer.Send</c> calls and contains no consumer, so it
    /// exercises <c>EnqueueLua</c> on the send path. Its doc comment names
    /// "ReceiveMessageQueryHandler.Handle", which is how the belief that receive was covered got
    /// started. The send path already has an async implementation
    /// (<c>SendMessageCommandHandlerAsync</c>), so it measures the sync API of a path that already
    /// has an alternative, and no change to the receive path can move its result.
    ///
    /// The mechanism is not "our thread is blocked". Consumers run <c>MainLoop</c> on dedicated
    /// threads (<c>Task.Factory.StartNew(MainLoop, TaskCreationOptions.LongRunning)</c>) — measured
    /// at zero of twenty-seven receives on a pool thread — so a blocking receive occupies no pool
    /// thread. What starves is StackExchange.Redis: a synchronous <c>ScriptEvaluate</c> is
    /// sync-over-async internally and needs a pool thread to run the completion satisfying its own
    /// blocking wait. Starve it of that thread and the wait outlives syncTimeout.
    ///
    /// The two tests here separate the two ways that can happen, because they are not equivalent:
    ///
    /// - <see cref="StarvationReceive_CappedPoolAlone_Diagnostic"/> caps the pool and
    ///   changes nothing else. It PASSES. The send-side baseline reproduces because its senders run
    ///   ON the capped pool; a dedicated-thread caller leaves those threads free for completions.
    /// - <see cref="StarvationReceive_ExternallySaturatedPool_SyncDequeue_FailsWithTimeout"/> holds
    ///   the pool with unrelated work once the consumer is already running. That is the condition
    ///   #161 actually recorded
    ///   (<c>WORKER: (Busy=215, Min=200)</c>) — pressure from elsewhere, which on this CI means
    ///   thirteen parallel Jenkins stages.
    ///
    /// Messages are always produced BEFORE the pool is interfered with. That is what lets a later
    /// timeout be attributed to the receive: with sends completed under a healthy pool, a timeout
    /// afterwards is not a send timeout. The assertions look only at
    /// <c>ErrorReceiveNotification</c>, which <c>MessageProcessingAsync</c> raises solely from its
    /// <c>ReceiveMessageException</c> handler; commit and rollback failures arrive on different
    /// notifications and are reported, not asserted on.
    ///
    /// Both carry two categories on purpose. StarvationBaseline is what the Jenkinsfile already
    /// excludes (<c>--filter "TestCategory!=StarvationBaseline"</c>), so neither needs a CI change
    /// while the gate is expected RED. StarvationReceive selects this file alone:
    ///     dotnet test ... --filter "TestCategory=StarvationReceive"
    ///
    /// Thread-pool settings are process-global and are ALWAYS restored, max before min — see the
    /// comment at the restore; the order is not cosmetic.
    ///
    /// MEASURED, and both are still RED after the async receive landed — read this before
    /// changing either assertion:
    ///
    /// <code>
    ///                                   sync receive   async receive
    ///   gate (saturated, cap 6)             478              25
    ///   control (cap 6 alone)                 0          15 / 23 / 25
    ///   control, cap raised to 64             -               0  (passes)
    /// </code>
    ///
    /// The 478 -> 25 under saturation is the fix working. The residue is pool capacity, not a
    /// defect: a cap of six cannot service twenty-five workers, because async continuations
    /// still need a pool thread even though nothing blocks on one. Raising the cap to
    /// sixty-four takes the same scenario to zero, which is what proves it.
    ///
    /// So both assertions are now calibrated to reproduce the defect rather than to judge the
    /// fix, and a zero-symptom bar is unreachable by construction at cap 6. Deliberately left
    /// alone rather than retuned: every number above was measured under WSL, which cannot run
    /// this suite reliably (the same commit fails there and passes on Windows), so any
    /// threshold set from them would be guesswork. Recalibrate on Windows or a Jenkins agent.
    /// Until then these behave like their send-side sibling: permanent red diagnostics,
    /// excluded from CI, kept for the mechanism they document.
    ///
    /// <c>[DoNotParallelize]</c> is load-bearing, not tidiness. This assembly runs
    /// <c>Parallelize(Workers = 2, Scope = MethodLevel)</c>, and the first run of these two let
    /// the gate's saturation land on the control's warm-up: the control failed having processed
    /// zero messages "under a healthy pool" that the other test had already taken. Two tests that
    /// both rewrite ThreadPool min/max cannot share a process concurrently.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class StarvationReceiveBaselineTests
    {
        // Cap worker threads to this value. Low enough that concurrent de-queue completions cannot
        // all get a thread, high enough that the consumer's start-up machinery and the test harness
        // can still acquire one. Matches the send-side baseline's cap.
        private const int WorkerCap = 6;

        // Consumer workers. Each gets a DEDICATED thread (LongRunning), so this is deliberately not
        // bounded by WorkerCap - that asymmetry is the subject of the control below.
        private const int WorkerCount = 25;

        // Messages pre-loaded before the pool is touched. Enough that every worker finds work and
        // keeps issuing de-queue EVALs, rather than parking in the empty-queue subscription wait -
        // the Redis receive is IsBlockingOperation, and a worker waiting on the work signal is not
        // exercising EVAL at all.
        private const int MessageCount = 2000;

        // Messages the consumer must process under a HEALTHY pool before pressure is applied,
        // proving the connection is up and the de-queue loop is turning.
        private const int WarmUpMessages = 20;

        // Hard bound on each observation window. Under starvation the interesting event is a
        // timeout; if none arrives we want a clean verdict rather than a hung agent.
        private static readonly TimeSpan ObservationWindow = TimeSpan.FromSeconds(45);

        /// <summary>
        /// The control for the gate below, and it changed meaning when the receive became asynchronous.
        ///
        /// It PASSED before that change: capping the pool alone produced zero receive timeouts, because
        /// a synchronous receive blocks a dedicated worker thread and leaves the six pool threads free
        /// to run completions. The send-side baseline reproduces only because its senders run ON the
        /// capped pool, so the caller occupying a pool thread IS the deadlock.
        ///
        /// It is RED now, at 15 to 25 symptoms, and that is expected rather than a regression: async
        /// continuations need a pool thread even though nothing blocks on one, and six threads cannot
        /// service twenty-five workers. Raising the cap to sixty-four returns it to zero, which is what
        /// shows the residue is capacity rather than a defect. The zero assertion is kept deliberately,
        /// so the number stays visible in the failure text instead of being tuned out of sight.
        ///
        /// Historical note on the mechanism, which still holds. The send-side baseline
        /// reproduces because its fifty senders are <c>new Task(...)</c> running ON the capped pool:
        /// six threads block inside <c>ScriptEvaluate</c> and the completion that would release them
        /// needs a seventh. The caller occupying a pool thread IS the deadlock.
        ///
        /// Consumer workers occupy none of the six, so they stay free to run completions. The
        /// receive path is structurally immune to the self-inflicted deadlock the send path suffers.
        ///
        /// Keep this. If it ever goes red, something has moved the de-queue onto a pool thread and
        /// the reasoning behind the gate below no longer holds.
        /// </summary>
        [TestMethod]
        [TestCategory("StarvationBaseline")]
        [TestCategory("StarvationReceive")]
        public void StarvationReceive_CappedPoolAlone_Diagnostic()
        {
            var outcome = RunConsumerUnderPressure(saturatePool: false);

            Assert.IsEmpty(outcome.EvalTimeouts,
                $"Control went RED: capping the pool alone starved the receive, which contradicts " +
                $"the reasoning the gate is built on. Something has likely moved the de-queue onto " +
                $"a pool thread.\n{outcome}");

            Assert.IsEmpty(outcome.OtherReceiveErrors,
                $"Control saw receive errors that are not EVAL timeouts, so this run is " +
                $"inconclusive: {outcome.OtherReceiveErrors.First()}\n{outcome}");
        }

        /// <summary>
        /// THE GATE. Expected RED while the de-queue is synchronous, GREEN once it awaits
        /// <c>DequeueLua.ExecuteAsync</c> (#256).
        ///
        /// The consumer is warmed up under a healthy pool first, then unrelated work takes every
        /// worker thread - modelling #161's <c>WORKER: (Busy=215, Min=200)</c>, pressure arriving
        /// at a consumer that is already running. Saturating before start-up only reproduces a
        /// connection failure, since SE.Redis needs pool threads to connect at all.
        ///
        /// The saturating items self-release on a timer, so the test cannot wedge the agent even if
        /// an assertion path is skipped.
        /// </summary>
        [TestMethod]
        [TestCategory("StarvationBaseline")]
        [TestCategory("StarvationReceive")]
        public void StarvationReceive_ExternallySaturatedPool_SyncDequeue_FailsWithTimeout()
        {
            var outcome = RunConsumerUnderPressure(saturatePool: true);

            // A drained queue means the workers spent the window parked on the work signal
            // rather than issuing de-queue EVALs, so a clean result proves nothing. Say so
            // instead of passing quietly - a green that means "we never tested it" is worse
            // than a red.
            if (outcome.EvalTimeouts.Count == 0 && outcome.Processed >= MessageCount)
            {
                Assert.Inconclusive(
                    $"Queue drained before the window elapsed, so the de-queue was idle for " +
                    $"most of it and no conclusion can be drawn. Raise MessageCount.\n{outcome}");
            }

            Assert.IsEmpty(outcome.EvalTimeouts,
                $"Receive-side thread-pool starvation reproduced: {outcome.EvalTimeouts.Count} " +
                $"de-queue timeout(s) out of the synchronous ScriptEvaluate path, with the pool " +
                $"capped at {WorkerCap}, held by unrelated work, and {WorkerCount} consumer " +
                $"workers. This is the expected RED until the de-queue awaits " +
                $"DequeueLua.ExecuteAsync (#256).\n{outcome}\n" +
                $"First timeout: {outcome.EvalTimeouts.First().Message}");

            Assert.IsEmpty(outcome.OtherReceiveErrors,
                $"Receive errors occurred that are not EVAL timeouts, so this run is inconclusive " +
                $"rather than a clean reproduction: {outcome.OtherReceiveErrors.First()}\n{outcome}");
        }

        private sealed class Outcome
        {
            public System.Collections.Generic.List<Exception> EvalTimeouts { get; init; }
            public System.Collections.Generic.List<Exception> OtherReceiveErrors { get; init; }
            public int Processed { get; init; }
            public int OtherErrors { get; init; }
            public int Cancellations { get; init; }
            public TimeSpan Elapsed { get; init; }

            public override string ToString() =>
                $"processed {Processed}/{MessageCount}, starvation symptoms {EvalTimeouts.Count}, " +
                $"unrelated receive errors {OtherReceiveErrors.Count}, shutdown cancellations " +
                $"{Cancellations}, non-receive errors {OtherErrors}, " +
                $"elapsed {Elapsed.TotalSeconds:F1}s" +
                $"\n  symptom types: {Describe(EvalTimeouts)}" +
                $"\n  unrelated types: {Describe(OtherReceiveErrors)}";

            private static string Describe(System.Collections.Generic.IEnumerable<Exception> errors)
            {
                var grouped = errors
                    .GroupBy(e =>
                    {
                        var inner = e.InnerException?.GetType().Name ?? "none";
                        return e.GetType().Name + " -> " + inner;
                    })
                    .Select(g => g.Key + " x" + g.Count())
                    .ToList();

                return grouped.Count == 0 ? "none" : string.Join(", ", grouped);
            }
        }

        private static Outcome RunConsumerUnderPressure(bool saturatePool)
        {
            var queueConnection = new QueueConnection(GenerateQueueName.Create(), ConnectionInfo.ConnectionString);

            using var queueCreator = new QueueCreationContainer<RedisQueueInit>();
            using var creation = queueCreator.GetQueueCreation<RedisQueueCreation>(queueConnection);
            var created = creation.CreateQueue();
            Assert.IsTrue(created.Success, created.ErrorMessage);

            try
            {
                // --- Pre-load under a HEALTHY pool -------------------------------------------
                // These must succeed. A send failure here makes the run inconclusive rather than
                // red, and would otherwise be mistaken for the receive starvation under test.
                using (var producerContainer = new QueueContainer<RedisQueueInit>())
                using (var producer = producerContainer.CreateProducer<FakeMessage>(queueConnection))
                {
                    for (var i = 0; i < MessageCount; i++)
                    {
                        var sent = producer.Send(new FakeMessage { Name = Guid.NewGuid().ToString() });
                        Assert.IsFalse(sent.HasError,
                            $"Pre-load send {i} failed under an uncapped pool, so this run cannot " +
                            $"attribute anything to the receive path: {sent.SendingException}");
                    }
                }

                var receiveErrors = new ConcurrentQueue<Exception>();
                var otherErrors = new ConcurrentQueue<Exception>();
                var processed = 0;
                var firstReceiveError = new ManualResetEventSlim(false);

                var notifications = new ConsumerQueueNotifications(
                    onError: n =>
                    {
                        if (n?.Error != null) otherErrors.Enqueue(n.Error);
                    },
                    onReceiveMessageError: n =>
                    {
                        if (n?.Error == null) return;
                        receiveErrors.Enqueue(n.Error);
                        firstReceiveError.Set();
                    });

                ThreadPool.GetMinThreads(out var origMinWorker, out var origMinIocp);
                ThreadPool.GetMaxThreads(out var origMaxWorker, out var origMaxIocp);

                var releaseSaturation = new ManualResetEventSlim(false);
                var sw = Stopwatch.StartNew();
                try
                {
                    using var consumerContainer = new QueueContainer<RedisQueueInit>();
                    using var consumer = consumerContainer.CreateConsumerAsync(queueConnection);
                    consumer.Configuration.Worker.WorkerCount = WorkerCount;

                    consumer.Start<FakeMessage>((message, workerNotification) =>
                    {
                        // Deliberately trivial. Real work here would add pool pressure of its own
                        // and blur what a timeout gets attributed to.
                        Interlocked.Increment(ref processed);
                        return Task.CompletedTask;
                    }, notifications);

                    // Let the consumer connect and get its de-queue loop turning BEFORE touching
                    // the pool. Saturating first does not reproduce anything useful: SE.Redis needs
                    // pool threads to establish a connection at all, so an earlier version of this
                    // test died on RedisConnectionException/ConnectTimeout before a single EVAL was
                    // issued. Pressure arriving at an already-running consumer is also the real
                    // scenario - thirteen parallel Jenkins stages, not a cold start.
                    var warm = SpinWaitFor(() => Volatile.Read(ref processed) >= WarmUpMessages,
                        TimeSpan.FromSeconds(30));
                    Assert.IsTrue(warm,
                        $"Consumer did not process {WarmUpMessages} messages under a healthy pool " +
                        $"within 30s (saw {Volatile.Read(ref processed)}), so this run cannot " +
                        $"attribute anything to pool pressure.");

                    // Order: lower MIN first, then MAX - max >= min is required at all times.
                    // IOCP threads are left alone; the contention reproduced here is on workers.
                    ThreadPool.SetMinThreads(WorkerCap, origMinIocp);
                    ThreadPool.SetMaxThreads(WorkerCap, origMaxIocp);

                    if (saturatePool)
                        HoldThePool(releaseSaturation);

                    // Stop at the first receive error, else run the window out. Draining every
                    // message is not the success condition - the absence of a receive timeout is.
                    firstReceiveError.Wait(ObservationWindow);
                }
                finally
                {
                    releaseSaturation.Set();

                    // Restore MAX before MIN. The pool is pinned at min=max=WorkerCap here, so
                    // restoring min first fails silently (returns false, because origMin > current
                    // max) and leaves the process pinned low, corrupting every later test in the
                    // same run.
                    ThreadPool.SetMaxThreads(origMaxWorker, origMaxIocp);
                    ThreadPool.SetMinThreads(origMinWorker, origMinIocp);
                    sw.Stop();
                }

                var timeouts = receiveErrors.Where(IsStarvationSymptom).ToList();
                var remainder = receiveErrors.Except(timeouts).ToList();
                var cancellations = remainder.Where(IsShutdownCancellation).ToList();
                return new Outcome
                {
                    EvalTimeouts = timeouts,
                    OtherReceiveErrors = remainder.Except(cancellations).ToList(),
                    Cancellations = cancellations.Count,
                    Processed = Volatile.Read(ref processed),
                    OtherErrors = otherErrors.Count,
                    Elapsed = sw.Elapsed
                };
            }
            finally
            {
                // Best effort - may itself fail while the pool recovers. The verdict is already
                // decided by this point.
                try { creation.RemoveQueue(); } catch { /* intentional */ }
            }
        }

        /// <summary>
        /// Occupies every capped worker thread with unrelated blocking work, so a synchronous
        /// ScriptEvaluate has no thread available to run the completion that would release it.
        ///
        /// Queued at twice the cap so the queue stays backed up as items are released, and every
        /// item is bounded by the release signal AND a hard timeout - a saturator that outlives the
        /// test would poison the rest of the run.
        /// </summary>
        private static void HoldThePool(ManualResetEventSlim release)
        {
            for (var i = 0; i < WorkerCap * 3; i++)
            {
                ThreadPool.UnsafeQueueUserWorkItem(
                    _ => release.Wait(ObservationWindow + TimeSpan.FromSeconds(15)), null);
            }

            // Confirm saturation by OBSERVING the pool, not by waiting for our own items to run.
            // An earlier version counted our items in and failed when fewer than the cap were
            // scheduled - but that is precisely what happens when the pool is already full, so it
            // could not tell "I failed to saturate it" from "it was saturated before I arrived".
            // Either is a valid starting condition; what matters is that no worker thread is free.
            var deadline = DateTime.UtcNow.AddSeconds(30);
            int available;
            do
            {
                ThreadPool.GetAvailableThreads(out available, out _);
                if (available == 0) return;
                Thread.Sleep(50);
            } while (DateTime.UtcNow < deadline);

            Assert.AreEqual(0, available,
                $"Could not saturate the capped thread pool ({available} of {WorkerCap} worker " +
                $"threads still free after 30s), so this run proves nothing either way.");
        }

        private static bool SpinWaitFor(Func<bool> condition, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (condition()) return true;
                Thread.Sleep(25);
            }
            return condition();
        }

        /// <summary>
        /// Both RedisTimeoutException and RedisConnectionException count. An earlier version
        /// matched only EVAL/SCRIPT timeouts and left 800 of 1100 receive errors unclassified,
        /// which made the run read as inconclusive when it had in fact reproduced: once the pool
        /// cannot service SE.Redis, the multiplexer reports the same starvation as connection
        /// failures too. Anything outside these two is genuinely unrelated and still disqualifies
        /// the run.
        /// </summary>
        /// <summary>
        /// Cancellations raised while the consumer stops are expected, not evidence of anything.
        /// The first clean run of the gate produced exactly 25 of them against a WorkerCount of
        /// 25 - one per worker, at disposal. Counting those as unrelated errors would have kept
        /// this test red for a bogus reason after the real fix landed, which is the failure mode
        /// a gate can least afford.
        /// </summary>
        private static bool IsShutdownCancellation(Exception ex)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                if (e is OperationCanceledException) return true;
            }
            return false;
        }

        private static bool IsStarvationSymptom(Exception ex)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                if (e is RedisTimeoutException || e is RedisConnectionException) return true;
                if (e.Message.Contains("Timeout performing EVAL") ||
                    e.Message.Contains("Timeout performing SCRIPT") ||
                    e.Message.Contains("No connection is available")) return true;
            }
            return false;
        }
    }
}
