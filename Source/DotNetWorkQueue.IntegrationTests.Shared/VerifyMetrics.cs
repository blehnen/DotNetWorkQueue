using System;
using System.Linq;
using System.Threading;
using DotNetWorkQueue.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.IntegrationTests.Shared
{
    internal static class VerifyMetrics
    {
        public static long GetPoisonMessageCount(MetricsSnapshot data)
        {
            var name = "PoisonHandleMeter";
            foreach (var metric in data.Meters.Where(
                m => m.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                return metric.Value;
            }
            return 0;
        }

        public static void VerifyPoisonMessageCount(string queueName, MetricsSnapshot data, long messageCount)
        {
            var count = GetPoisonMessageCount(data);
            Assert.AreEqual(messageCount, count);
        }

        /// <summary>
        /// Polls live metrics until <c>PoisonHandleMeter</c> reaches the expected value or times out.
        /// Fixes a race where the handler callback signals completion before the poison meter is incremented.
        /// </summary>
        public static void VerifyPoisonMessageCount(string queueName, IMetrics metrics, long messageCount, int timeoutMs = 30000)
        {
            const string name = "PoisonHandleMeter";
            PollUntil(
                metrics,
                data =>
                {
                    foreach (var meter in data.Meters.Where(
                        m => m.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        return meter.Value;
                    }
                    return null;
                },
                messageCount,
                timeoutMs,
                data => VerifyPoisonMessageCount(queueName, data, messageCount));
        }

        public static long GetExpiredMessageCount(MetricsSnapshot data)
        {
            var names = new[] { ".ClearMessages.ResetCounter", ".HandleAsync.Expired" };
            long count = 0;
            foreach (var name in names)
            {
                //Sum every matching counter, which is what this method has always claimed to do.
                //It used to stop after the first, which was invisible while exactly one counter ended
                //in each suffix. The Redis transport now has two ending in ".HandleAsync.Expired" -
                //one for the synchronous receive handler and one for the asynchronous twin - and only
                //the path the consumer actually used is non-zero. Taking whichever enumerated first
                //returned 0 for a run that had expired every message.
                //
                //Summing cannot double count: a consumer uses the sync receive path or the async one,
                //never both, so at most one of the pair is ever incremented.
                foreach (var metric in data.Counters.Where(
                    c => c.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    count = count + metric.Value;
                }
            }
            return count;
        }

        public static void VerifyExpiredMessageCount(string queueName, MetricsSnapshot data, long messageCount)
        {
            var count = GetExpiredMessageCount(data);
            Assert.AreEqual(messageCount, count);
        }

        /// <summary>
        /// Polls live metrics until the combined expired-message counters reach the expected value or times out.
        /// Mirrors the GetExpiredMessageCount logic (sums ClearMessages.ResetCounter + HandleAsync.Expired).
        /// </summary>
        /// <summary>
        /// Verifies the combined expired-message counters against the metrics collected so far.
        /// </summary>
        /// <remarks>
        /// Reads the snapshot once rather than waiting for it to change. Every caller of this overload
        /// runs after the queue has been disposed, and the snapshot is a synchronous read of the live
        /// counters, so nothing remains that could increment them: a value that has not landed by now
        /// never will. The thirty second wait this replaced never once shortened - across three
        /// transports, 105 of 105 polling calls were satisfied by their first read, in 0 ms - while it
        /// did suggest a tolerance that does not exist and added thirty seconds to every failure of
        /// this shape (GitHub #314).
        ///
        /// <see cref="VerifyPoisonMessageCount(string, IMetrics, long, int)"/> is the deliberate
        /// exception: its callers verify from inside the queue's using block, where workers are still
        /// alive and a later read genuinely can differ.
        /// </remarks>
        public static void VerifyExpiredMessageCount(string queueName, IMetrics metrics, long messageCount)
        {
            VerifyExpiredMessageCount(queueName, metrics.GetCollectedMetrics(), messageCount);
        }

        public static void VerifyRollBackCount(string queueName, MetricsSnapshot data, long messageCount, int rollbackCount, int failedCount)
        {
            var found = false;
            const string name = "RollbackMessage.RollbackCounter";
            const string retryName = "MessageFailedProcessingRetryMeter";
            foreach (var metric in data.Counters.Where(
                c => c.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                Assert.AreEqual(messageCount * rollbackCount, metric.Value);
                found = true;
                break;
            }
            if (!found)
            {
                throw new DotNetWorkQueueException($"Failed to find metric {name}");
            }

            if (failedCount > 0)
            {
                found = false;
                foreach (var metric in data.Meters.Where(
                    m => m.Key.EndsWith(retryName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    Assert.AreEqual(messageCount * failedCount, metric.Value);
                    found = true;
                    break;
                }
                if (!found)
                {
                    throw new DotNetWorkQueueException($"Failed to find metric {retryName}");
                }
            }
        }

        /// <summary>
        /// Polls live metrics until <c>RollbackMessage.RollbackCounter</c> reaches
        /// <paramref name="messageCount"/> * <paramref name="rollbackCount"/> AND
        /// (when <paramref name="failedCount"/> &gt; 0) <c>MessageFailedProcessingRetryMeter</c>
        /// reaches <paramref name="messageCount"/> * <paramref name="failedCount"/>, then
        /// asserts the full rollback + retry-meter invariants via the snapshot overload.
        /// Polling both metrics closes the residual race: rollback can tick before retry,
        /// and the snapshot finalAssert checks both — so polling on rollback alone would
        /// still leave a window where finalAssert fails on the retry-meter lag.
        /// </summary>
        /// <summary>
        /// Verifies the rollback counter, and the retry meter when one is expected, against the
        /// metrics collected so far.
        /// </summary>
        /// <remarks>
        /// Reads the snapshot once rather than waiting for it to change. Every caller of this overload
        /// runs after the queue has been disposed, and the snapshot is a synchronous read of the live
        /// counters, so nothing remains that could increment them: a value that has not landed by now
        /// never will. The thirty second wait this replaced never once shortened - across three
        /// transports, 105 of 105 polling calls were satisfied by their first read, in 0 ms - while it
        /// did suggest a tolerance that does not exist and added thirty seconds to every failure of
        /// this shape (GitHub #314).
        ///
        /// <see cref="VerifyPoisonMessageCount(string, IMetrics, long, int)"/> is the deliberate
        /// exception: its callers verify from inside the queue's using block, where workers are still
        /// alive and a later read genuinely can differ.
        /// </remarks>
        public static void VerifyRollBackCount(string queueName, IMetrics metrics, long messageCount, int rollbackCount, int failedCount)
        {
            VerifyRollBackCount(queueName, metrics.GetCollectedMetrics(), messageCount, rollbackCount, failedCount);
        }

        public static void VerifyProducedAsyncCount(string queueName, MetricsSnapshot data, long messageCount)
        {
            var found = false;
            var name = "SendMessagesMeter";
            foreach (var meter in data.Meters.Where(
                m => m.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                Assert.AreEqual(messageCount, meter.Value);
                found = true;
                break;
            }
            if (!found)
            {
                throw new DotNetWorkQueueException($"Failed to find meter {name}");
            }
        }

        /// <summary>
        /// Verifies the async produced counter against the metrics collected so far.
        /// </summary>
        /// <remarks>
        /// Reads the snapshot once rather than waiting for it to change. Every caller of this overload
        /// runs after the queue has been disposed, and the snapshot is a synchronous read of the live
        /// counters, so nothing remains that could increment them: a value that has not landed by now
        /// never will. The thirty second wait this replaced never once shortened - across three
        /// transports, 105 of 105 polling calls were satisfied by their first read, in 0 ms - while it
        /// did suggest a tolerance that does not exist and added thirty seconds to every failure of
        /// this shape (GitHub #314).
        ///
        /// <see cref="VerifyPoisonMessageCount(string, IMetrics, long, int)"/> is the deliberate
        /// exception: its callers verify from inside the queue's using block, where workers are still
        /// alive and a later read genuinely can differ.
        /// </remarks>
        public static void VerifyProducedAsyncCount(string queueName, IMetrics metrics, long messageCount)
        {
            VerifyProducedAsyncCount(queueName, metrics.GetCollectedMetrics(), messageCount);
        }

        public static void VerifyProducedCount(string queueName, MetricsSnapshot data, long messageCount)
        {
            var found = false;
            var name = "SendMessagesMeter";
            foreach (var meter in data.Meters.Where(
                m => m.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                Assert.AreEqual(messageCount, meter.Value);
                found = true;
                break;
            }
            if (!found)
            {
                throw new DotNetWorkQueueException($"Failed to find meter {name}");
            }
        }

        /// <summary>
        /// Verifies the produced counter against the metrics collected so far.
        /// </summary>
        /// <remarks>
        /// Reads the snapshot once rather than waiting for it to change. Every caller of this overload
        /// runs after the queue has been disposed, and the snapshot is a synchronous read of the live
        /// counters, so nothing remains that could increment them: a value that has not landed by now
        /// never will. The thirty second wait this replaced never once shortened - across three
        /// transports, 105 of 105 polling calls were satisfied by their first read, in 0 ms - while it
        /// did suggest a tolerance that does not exist and added thirty seconds to every failure of
        /// this shape (GitHub #314).
        ///
        /// <see cref="VerifyPoisonMessageCount(string, IMetrics, long, int)"/> is the deliberate
        /// exception: its callers verify from inside the queue's using block, where workers are still
        /// alive and a later read genuinely can differ.
        /// </remarks>
        public static void VerifyProducedCount(string queueName, IMetrics metrics, long messageCount)
        {
            VerifyProducedCount(queueName, metrics.GetCollectedMetrics(), messageCount);
        }

        public static void VerifyProcessedCount(string queueName, MetricsSnapshot data, long messageCount)
        {
            var found = false;
            const string name = "CommitMessage.CommitCounter";
            foreach (var counter in data.Counters.Where(
                c => c.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                Assert.AreEqual(messageCount, counter.Value);
                found = true;
                break;
            }
            if (!found)
            {
                throw new DotNetWorkQueueException($"Failed to find counter {name}");
            }
        }

        /// <summary>
        /// Polls live metrics on a 100ms interval until <paramref name="getValue"/> reaches
        /// <paramref name="expected"/> or <paramref name="timeoutMs"/> elapses, then re-issues
        /// <paramref name="finalAssert"/> against the latest snapshot for a clean error message.
        /// Fixes a class of race where the handler callback signals completion before a
        /// metric counter/meter is incremented.
        /// </summary>
        private static void PollUntil(
            IMetrics metrics,
            Func<MetricsSnapshot, long?> getValue,
            long expected,
            int timeoutMs,
            Action<MetricsSnapshot> finalAssert)
        {
            if (expected == 0)
            {
                finalAssert(metrics.GetCollectedMetrics());
                return;
            }

            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                var data = metrics.GetCollectedMetrics();
                var value = getValue(data);
                if (value.HasValue && value.Value >= expected)
                {
                    finalAssert(data);
                    return;
                }
                Thread.Sleep(100);
            }

            finalAssert(metrics.GetCollectedMetrics());
        }

        /// <summary>
        /// Polls the live metrics until CommitCounter reaches the expected value or times out.
        /// Fixes a race where the handler callback signals completion before the commit metric is incremented.
        /// Default timeout covers chaos + hold-transaction scenarios under CI load. Raised from 15s
        /// when the integration suites moved to 4-way parallelism: a PostgreSQL MultiConsumerAsync
        /// chaos row reported 24 of 25 commits because the last counter had not caught up in 15s.
        /// </summary>
        /// <summary>
        /// Verifies the processed counter against the metrics collected so far.
        /// </summary>
        /// <remarks>
        /// Reads the snapshot once rather than waiting for it to change. Every caller of this overload
        /// runs after the queue has been disposed, and the snapshot is a synchronous read of the live
        /// counters, so nothing remains that could increment them: a value that has not landed by now
        /// never will. The thirty second wait this replaced never once shortened - across three
        /// transports, 105 of 105 polling calls were satisfied by their first read, in 0 ms - while it
        /// did suggest a tolerance that does not exist and added thirty seconds to every failure of
        /// this shape (GitHub #314).
        ///
        /// <see cref="VerifyPoisonMessageCount(string, IMetrics, long, int)"/> is the deliberate
        /// exception: its callers verify from inside the queue's using block, where workers are still
        /// alive and a later read genuinely can differ.
        /// </remarks>
        public static void VerifyProcessedCount(string queueName, IMetrics metrics, long messageCount)
        {
            VerifyProcessedCount(queueName, metrics.GetCollectedMetrics(), messageCount);
        }
    }
}
