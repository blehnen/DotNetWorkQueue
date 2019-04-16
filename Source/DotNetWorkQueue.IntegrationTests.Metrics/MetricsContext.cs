using System;
using System.Collections.Generic;
using DotNetWorkQueue.Metrics.NoOp;

namespace DotNetWorkQueue.IntegrationTests.Metrics
{
    /// <inheritdoc />
    internal class MetricsContext : IMetricsContext
    {
        private readonly Metrics _metrics;

        public MetricsContext()
        {
            _metrics = new Metrics(string.Empty);
        }

        /// <inheritdoc />
        public IMetricsContext Context(string contextName)
        {
            return new MetricsContext();
        }

        /// <inheritdoc />
        public void ShutdownContext(string contextName)
        {
           
        }

        /// <inheritdoc />
        public void Gauge(string name, Func<double> valueProvider, Units unit, List<KeyValuePair<string, string>> tags = null)
        {
           
        }

        /// <inheritdoc />
        public ICounter Counter(string name, Units unit, List<KeyValuePair<string, string>> tags = null)
        {
            return _metrics.Counter(name, unit, tags);
        }

        /// <inheritdoc />
        public ICounter Counter(string name, string unitName, List<KeyValuePair<string, string>> tags = null)
        {
            return _metrics.Counter(name, unitName, tags);
        }

        /// <inheritdoc />
        public IMeter Meter(string name, Units unit, TimeUnits rateUnit = TimeUnits.Seconds, List<KeyValuePair<string, string>> tags = null)
        {
            return _metrics.Meter(name, unit, rateUnit, tags);
        }

        /// <inheritdoc />
        public IMeter Meter(string name, string unitName, TimeUnits rateUnit, List<KeyValuePair<string, string>> tags = null)
        {
            return _metrics.Meter(name, unitName, rateUnit, tags);
        }

        /// <inheritdoc />
        public IHistogram Histogram(string name, Units unit, SamplingTypes samplingType = SamplingTypes.FavorRecent, List<KeyValuePair<string, string>> tags = null)
        {
            return new HistogramNoOp();
        }

        /// <inheritdoc />
        public ITimer Timer(string name, Units unit, SamplingTypes samplingType = SamplingTypes.FavorRecent, TimeUnits rateUnit = TimeUnits.Seconds, TimeUnits durationUnit = TimeUnits.Milliseconds, List<KeyValuePair<string, string>> tags = null)
        {
            return new TimerNoOp();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _metrics.Dispose();
        }
    }
}
