using System;
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
        public void Gauge(string name, Func<double> valueProvider, Units unit, string tag = null)
        {
           
        }

        /// <inheritdoc />
        public ICounter Counter(string name, Units unit, string tag = null)
        {
            return _metrics.Counter(name, unit, tag);
        }

        /// <inheritdoc />
        public ICounter Counter(string name, string unitName, string tag = null)
        {
            return _metrics.Counter(name, unitName, tag);
        }

        /// <inheritdoc />
        public IMeter Meter(string name, Units unit, TimeUnits rateUnit = TimeUnits.Seconds, string tag = null)
        {
            return _metrics.Meter(name, unit, rateUnit, tag);
        }

        /// <inheritdoc />
        public IMeter Meter(string name, string unitName, TimeUnits rateUnit, string tag = null)
        {
            return _metrics.Meter(name, unitName, rateUnit, tag);
        }

        /// <inheritdoc />
        public IHistogram Histogram(string name, Units unit, SamplingTypes samplingType = SamplingTypes.FavorRecent, string tag = null)
        {
            return new HistogramNoOp();
        }

        /// <inheritdoc />
        public ITimer Timer(string name, Units unit, SamplingTypes samplingType = SamplingTypes.FavorRecent, TimeUnits rateUnit = TimeUnits.Seconds, TimeUnits durationUnit = TimeUnits.Milliseconds, string tag = null)
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
