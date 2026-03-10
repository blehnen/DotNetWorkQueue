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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace DotNetWorkQueue.Metrics.Net
{
    /// <inheritdoc />
    public class MetricsNet : IMetrics, IDisposable
    {
        private readonly Meter _meter;
        private readonly ConcurrentDictionary<string, CounterNet> _counters = new ConcurrentDictionary<string, CounterNet>();
        private readonly ConcurrentDictionary<string, MeterNet> _meters = new ConcurrentDictionary<string, MeterNet>();
        private readonly ConcurrentDictionary<string, IMetricsContext> _childContexts = new ConcurrentDictionary<string, IMetricsContext>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsNet"/> class.
        /// </summary>
        public MetricsNet()
        {
            _meter = new Meter("DotNetWorkQueue");
        }

        /// <inheritdoc />
        public IMetricsContext Context(string contextName)
        {
            return _childContexts.GetOrAdd(contextName,
                name => new MetricsContextNet(new Meter($"{_meter.Name}.{name}")));
        }

        /// <inheritdoc />
        public void ShutdownContext(string contextName)
        {
            if (_childContexts.TryRemove(contextName, out var context))
            {
                context.Dispose();
            }
        }

        /// <inheritdoc />
        public void Gauge(string name, Func<double> valueProvider, Units unit, List<KeyValuePair<string, string>> tags = null)
        {
            _meter.CreateObservableGauge(name, valueProvider, unit.ToString());
        }

        /// <inheritdoc />
        public ICounter Counter(string name, Units unit, List<KeyValuePair<string, string>> tags = null)
        {
            return _counters.GetOrAdd(name, n =>
            {
                var sdmTags = TagsHelper.Convert(tags);
                var counter = _meter.CreateUpDownCounter<long>(n, unit.ToString());
                return new CounterNet(counter, sdmTags);
            });
        }

        /// <inheritdoc />
        public ICounter Counter(string name, string unitName, List<KeyValuePair<string, string>> tags = null)
        {
            return _counters.GetOrAdd(name, n =>
            {
                var sdmTags = TagsHelper.Convert(tags);
                var counter = _meter.CreateUpDownCounter<long>(n, unitName);
                return new CounterNet(counter, sdmTags);
            });
        }

        /// <inheritdoc />
        public IMeter Meter(string name, Units unit, TimeUnits rateUnit = TimeUnits.Seconds, List<KeyValuePair<string, string>> tags = null)
        {
            return _meters.GetOrAdd(name, n =>
            {
                var sdmTags = TagsHelper.Convert(tags);
                var counter = _meter.CreateCounter<long>(n, unit.ToString());
                return new MeterNet(counter, sdmTags);
            });
        }

        /// <inheritdoc />
        public IMeter Meter(string name, string unitName, TimeUnits rateUnit, List<KeyValuePair<string, string>> tags = null)
        {
            return _meters.GetOrAdd(name, n =>
            {
                var sdmTags = TagsHelper.Convert(tags);
                var counter = _meter.CreateCounter<long>(n, unitName);
                return new MeterNet(counter, sdmTags);
            });
        }

        /// <inheritdoc />
        public IHistogram Histogram(string name, Units unit, List<KeyValuePair<string, string>> tags = null)
        {
            var sdmTags = TagsHelper.Convert(tags);
            var histogram = _meter.CreateHistogram<long>(name, unit.ToString());
            return new HistogramNet(histogram, sdmTags);
        }

        /// <inheritdoc />
        public ITimer Timer(string name, Units unit, TimeUnits rateUnit = TimeUnits.Seconds, TimeUnits durationUnit = TimeUnits.Milliseconds, List<KeyValuePair<string, string>> tags = null)
        {
            var sdmTags = TagsHelper.Convert(tags);
            var histogram = _meter.CreateHistogram<double>(name, "ms");
            return new TimerNet(histogram, sdmTags);
        }

        /// <inheritdoc />
        public MetricsSnapshot GetCollectedMetrics()
        {
            var counters = new Dictionary<string, long>();
            foreach (var kvp in _counters)
                counters[kvp.Key] = kvp.Value.Value;

            var meters = new Dictionary<string, long>();
            foreach (var kvp in _meters)
                meters[kvp.Key] = kvp.Value.Value;

            return new MetricsSnapshot(counters, meters);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var child in _childContexts.Values)
            {
                child.Dispose();
            }
            _childContexts.Clear();
            _meter.Dispose();
        }
    }
}
