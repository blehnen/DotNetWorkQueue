// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using DotNetWorkQueue.Exceptions;
using Metrics.MetricData;
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
                            counter => counter.Name.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    count = count + metric.Value.Count;
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
                            counter => counter.Name.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    count = count + metric.Value.Count;
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
            foreach (var metric in data.Counters.Where(counter => counter.Name.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                Assert.Equal(messageCount * rollbackCount, metric.Value.Count);
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
                            counter => counter.Name.EndsWith(retryName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    Assert.Equal(messageCount*failedCount, metric.Value.Count);
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
            foreach (var meter in data.Meters.Where(timer => timer.Name.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                Assert.Equal(messageCount, meter.Value.Count);
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
            foreach (var meter in data.Meters.Where(timer => timer.Name.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                Assert.Equal(messageCount, meter.Value.Count);
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
            const string name = "CommitMessage.CommitCounter";
            foreach (var counter in data.Counters.Where(counter => counter.Name.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                Assert.Equal(messageCount, counter.Value.Count);
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
