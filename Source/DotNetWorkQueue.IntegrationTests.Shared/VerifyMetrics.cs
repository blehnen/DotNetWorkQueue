using System;
using System.Linq;
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
    }
}
