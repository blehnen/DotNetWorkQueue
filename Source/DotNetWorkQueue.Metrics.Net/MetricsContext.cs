using System;
using Metrics;

namespace DotNetWorkQueue.Metrics.Net
{
    /// <inheritdoc />
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
        public void Gauge(string name, Func<double> valueProvider, Units unit, string tag = null)
        {
            _context.Gauge(name, valueProvider, unit.ToString(), tag);
        }

        /// <inheritdoc />
        public ICounter Counter(string name, Units unit, string tag = null)
        {
            return new Counter(_context.Counter(name, unit.ToString(), tag));
        }

        /// <inheritdoc />
        public ICounter Counter(string name, string unitName, string tag = null)
        {
            return new Counter(_context.Counter(name, unitName, tag));
        }

        /// <inheritdoc />
        public IMeter Meter(string name, Units unit, TimeUnits rateUnit = TimeUnits.Seconds, string tag = null)
        {
            return new Meter(_context.Meter(name, unit.ToString(), (TimeUnit)rateUnit, tag));
        }

        /// <inheritdoc />
        public IMeter Meter(string name, string unitName, TimeUnits rateUnit, string tag = null)
        {
            return new Meter(_context.Meter(name, unitName, (TimeUnit)rateUnit, tag));
        }

        /// <inheritdoc />
        public IHistogram Histogram(string name, Units unit, SamplingTypes samplingType = SamplingTypes.FavorRecent, string tag = null)
        {
            return new Histogram(_context.Histogram(name, unit.ToString(), (SamplingType)samplingType, tag));
        }

        /// <inheritdoc />
        public ITimer Timer(string name, Units unit, SamplingTypes samplingType = SamplingTypes.FavorRecent, TimeUnits rateUnit = TimeUnits.Seconds, TimeUnits durationUnit = TimeUnits.Milliseconds, string tag = null)
        {
            return new Timer(_context.Timer(name, unit.ToString(), (SamplingType)samplingType, (TimeUnit)rateUnit, (TimeUnit)durationUnit, tag));
        }

        /// <inheritdoc />
        public void Dispose()
        {
           _context.Dispose();
        }
    }
}
