using System;
using System.Collections.Generic;
using Metrics;

namespace DotNetWorkQueue.Metrics.Net
{
    /// <inheritdoc />
    [Obsolete("Replaced by DotNetWorkQueue.AppMetrics", false)]
    internal class MetricsContext : IMetricsContext
    {
        private readonly global::Metrics.MetricsContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsContext"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public MetricsContext(global::Metrics.MetricsContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public IMetricsContext Context(string contextName)
        {
            return new MetricsContext(_context.Context(contextName));
        }

        /// <inheritdoc />
        public void ShutdownContext(string contextName)
        {
            _context.ShutdownContext(contextName);
        }

        /// <inheritdoc />
        public void Gauge(string name, Func<double> valueProvider, Units unit, List<KeyValuePair<string, string>> tags = null)
        {
            _context.Gauge(name, valueProvider, unit.ToString(), tags.GetFirstTag());
        }

        /// <inheritdoc />
        public ICounter Counter(string name, Units unit, List<KeyValuePair<string, string>> tags = null)
        {
            return new Counter(_context.Counter(name, unit.ToString(), tags.GetFirstTag()));
        }

        /// <inheritdoc />
        public ICounter Counter(string name, string unitName, List<KeyValuePair<string, string>> tags = null)
        {
            return new Counter(_context.Counter(name, unitName, tags.GetFirstTag()));
        }

        /// <inheritdoc />
        public IMeter Meter(string name, Units unit, TimeUnits rateUnit = TimeUnits.Seconds, List<KeyValuePair<string, string>> tags = null)
        {
            return new Meter(_context.Meter(name, unit.ToString(), (TimeUnit)rateUnit, tags.GetFirstTag()));
        }

        /// <inheritdoc />
        public IMeter Meter(string name, string unitName, TimeUnits rateUnit, List<KeyValuePair<string, string>> tags = null)
        {
            return new Meter(_context.Meter(name, unitName, (TimeUnit)rateUnit, tags.GetFirstTag()));
        }

        /// <inheritdoc />
        public IHistogram Histogram(string name, Units unit, SamplingTypes samplingType = SamplingTypes.FavorRecent, List<KeyValuePair<string, string>> tags = null)
        {
            return new Histogram(_context.Histogram(name, unit.ToString(), (SamplingType)samplingType, tags.GetFirstTag()));
        }

        /// <inheritdoc />
        public ITimer Timer(string name, Units unit, SamplingTypes samplingType = SamplingTypes.FavorRecent, TimeUnits rateUnit = TimeUnits.Seconds, TimeUnits durationUnit = TimeUnits.Milliseconds, List<KeyValuePair<string, string>> tags = null)
        {
            return new Timer(_context.Timer(name, unit.ToString(), (SamplingType)samplingType, (TimeUnit)rateUnit, (TimeUnit)durationUnit, tags.GetFirstTag()));
        }

        /// <inheritdoc />
        public void Dispose()
        {
           _context.Dispose();
        }
    }
}
