using System;
using Metrics;

namespace DotNetWorkQueue.Metrics.Net
{
    /// <inheritdoc cref="IMetrics" />
    public class Metrics: IMetrics, IDisposable
    {
        private readonly global::Metrics.MetricsContext _context;
        /// <summary>
        /// Initializes a new instance of the <see cref="Metrics"/> class.
        /// </summary>
        /// <param name="rootName">Name of the root.</param>
        public Metrics(string rootName)
        {
            _context = Metric.Context(rootName);
        }
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public MetricsConfig Config => Metric.Config;

        /// <inheritdoc />
        public void Gauge(string name, Func<double> valueProvider, Units unit, string tag = null)
        {
            _context.Gauge(name, valueProvider, unit.ToString(), tag);
        }

        /// <inheritdoc />
        public IMeter Meter(string name, Units unit, TimeUnits rateUnit, string tag = null)
        {
            return new Meter(_context.Meter(name, unit.ToString(), (TimeUnit)rateUnit, tag));
        }

        /// <inheritdoc />
        public IMeter Meter(string name, string unitName, TimeUnits rateUnit, string tag = null)
        {
            return new Meter(_context.Meter(name, unitName, (TimeUnit)rateUnit, tag));
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
        public IHistogram Histogram(string name, Units unit, SamplingTypes samplingType, string tag = null)
        {
            return new Histogram(_context.Histogram(name, unit.ToString(), (SamplingType)samplingType, tag));
        }

        /// <inheritdoc />
        public ITimer Timer(string name, Units unit, SamplingTypes samplingType, TimeUnits rateUnit, TimeUnits durationUnit, string tag = null)
        {
            return new Timer(_context.Timer(name, unit.ToString(), (SamplingType)samplingType, (TimeUnit)rateUnit, (TimeUnit)durationUnit, tag));
        }

        /// <inheritdoc />
        public IMetricsContext Context(string contextName)
        {
            return new MetricsContext(_context.Context(contextName));
        }

        /// <inheritdoc />
        public dynamic CollectedMetrics => _context.DataProvider.CurrentMetricsData;

        /// <inheritdoc />
        public void ShutdownContext(string contextName)
        {
            _context.ShutdownContext(contextName);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
        }
    }
}
