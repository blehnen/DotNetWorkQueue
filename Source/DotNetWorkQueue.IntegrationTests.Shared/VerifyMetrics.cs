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
        public static void VerifyPoisonMessageCount(string queueName, IMetrics metrics, long messageCount, int timeoutMs = 15000)
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
                foreach (var metric in data.Counters.Where(
                    c => c.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    count = count + metric.Value;
                    break;
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
        public static void VerifyExpiredMessageCount(string queueName, IMetrics metrics, long messageCount, int timeoutMs = 15000)
        {
            PollUntil(
                metrics,
                data => (long?)GetExpiredMessageCount(data),
                messageCount,
                timeoutMs,
                data => VerifyExpiredMessageCount(queueName, data, messageCount));
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
        /// <paramref name="messageCount"/> * <paramref name="rollbackCount"/> or times out,
        /// then asserts the full rollback + retry-meter invariants in one pass via the snapshot overload.
        /// </summary>
        public static void VerifyRollBackCount(string queueName, IMetrics metrics, long messageCount, int rollbackCount, int failedCount, int timeoutMs = 15000)
        {
            const string name = "RollbackMessage.RollbackCounter";
            var expected = messageCount * rollbackCount;
            PollUntil(
                metrics,
                data =>
                {
                    foreach (var counter in data.Counters.Where(
                        c => c.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        return counter.Value;
                    }
                    return null;
                },
                expected,
                timeoutMs,
                data => VerifyRollBackCount(queueName, data, messageCount, rollbackCount, failedCount));
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

        public static void VerifyProducedAsyncCount(string queueName, IMetrics metrics, long messageCount, int timeoutMs = 15000)
        {
            const string name = "SendMessagesMeter";
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
                data => VerifyProducedAsyncCount(queueName, data, messageCount));
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

        public static void VerifyProducedCount(string queueName, IMetrics metrics, long messageCount, int timeoutMs = 15000)
        {
            const string name = "SendMessagesMeter";
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
                data => VerifyProducedCount(queueName, data, messageCount));
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
        /// Default timeout is generous enough to survive chaos + hold-transaction scenarios under CI load.
        /// </summary>
        public static void VerifyProcessedCount(string queueName, IMetrics metrics, long messageCount, int timeoutMs = 15000)
        {
            const string name = "CommitMessage.CommitCounter";
            PollUntil(
                metrics,
                data =>
                {
                    foreach (var counter in data.Counters.Where(
                        c => c.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        return counter.Value;
                    }
                    return null;
                },
                messageCount,
                timeoutMs,
                data => VerifyProcessedCount(queueName, data, messageCount));
        }
    }
}
