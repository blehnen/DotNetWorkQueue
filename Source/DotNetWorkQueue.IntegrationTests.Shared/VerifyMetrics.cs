using System;
using System.Collections.Generic;
using System.Linq;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.IntegrationTests.Metrics;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared
{
    internal static class VerifyMetrics
    {
        public static long GetPoisonMessageCount(MetricsData data)
        {
            var names = new List<string>(1) { "PoisonHandleMeter" };
            long count = 0;
            foreach (var name in names)
            {
                foreach (
                    var metric in
                        data.Meters.Where(
                            counter => counter.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    count = count + metric.Value.Value;
                    break;
                }
            }
            return count;
        }

        public static void VerifyPoisonMessageCount(string queueName, MetricsData data, long messageCount)
        {
            var count = GetPoisonMessageCount(data);
            Assert.Equal(messageCount, count);
        }
        public static long GetExpiredMessageCount(MetricsData data)
        {
            var names = new List<string>(2) { ".ClearMessages.ResetCounter", ".HandleAsync.Expired" };
            long count = 0;
            foreach (var name in names)
            {
                foreach (
                    var metric in
                        data.Counters.Where(
                            counter => counter.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    count = count + metric.Value.Value;
                    break;
                }
            }
            return count;
        }
        public static void VerifyExpiredMessageCount(string queueName, MetricsData data, long messageCount)
        {
            var count = GetExpiredMessageCount(data);
            Assert.Equal(messageCount, count);
        }
        public static void VerifyRollBackCount(string queueName, MetricsData data, long messageCount, int rollbackCount, int failedCount)
        {
            var found = false;
            const string name = "RollbackMessage.RollbackCounter";
            const string retryName = "MessageFailedProcessingRetryMeter";
            foreach (var metric in data.Counters.Where(counter => counter.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                Assert.Equal(messageCount * rollbackCount, metric.Value.Value);
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
                foreach (
                    var metric in
                        data.Meters.Where(
                            counter => counter.Key.EndsWith(retryName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    Assert.Equal(messageCount*failedCount, metric.Value.Value);
                    found = true;
                    break;
                }
                if (!found)
                {
                    throw new DotNetWorkQueueException($"Failed to find metric {name}");
                }
            }
        }
        public static void VerifyProducedAsyncCount(string queueName, MetricsData data, long messageCount)
        {
            var found = false;
            var name = "SendMessagesMeter";
            foreach (var meter in data.Meters.Where(timer => timer.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                Assert.Equal(messageCount, meter.Value.Value);
                found = true;
                break;
            }
            if (!found)
            {
                throw new DotNetWorkQueueException($"Failed to find timer {name}");
            }
        }
        public static void VerifyProducedCount(string queueName, MetricsData data, long messageCount)
        {
            var found = false;
            var name = "SendMessagesMeter";
            foreach (var meter in data.Meters.Where(timer => timer.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                Assert.Equal(messageCount, meter.Value.Value);
                found = true;
                break;
            }
            if (!found)
            {
                throw new DotNetWorkQueueException($"Failed to find timer {name}");
            }
        }
        public static void VerifyProcessedCount(string queueName, MetricsData data, long messageCount)
        {
            var found = false;
            const string name = "CommitMessageDecorator.CommitCounter";
            foreach (var counter in data.Counters.Where(counter => counter.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                Assert.Equal(messageCount, counter.Value.Value);
                found = true;
                break;
            }
            if (!found)
            {
                throw new DotNetWorkQueueException($"Failed to find timer {name}");
            }
        }
    }
}
