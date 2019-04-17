using System;
using System.Collections.Generic;
using Metrics;

namespace DotNetWorkQueue.Metrics.Net
{
    /// <inheritdoc cref="IMetrics" />
    [Obsolete("Replaced by DotNetWorkQueue.AppMetrics", false)]
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
        public void Gauge(string name, Func<double> valueProvider, Units unit, List<KeyValuePair<string, string>> tags = null)
        {
            _context.Gauge(name, valueProvider, unit.ToString(), tags.GetFirstTag());
        }

        /// <inheritdoc />
        public IMeter Meter(string name, Units unit, TimeUnits rateUnit, List<KeyValuePair<string, string>> tags = null)
        {
            return new Meter(_context.Meter(name, unit.ToString(), (TimeUnit)rateUnit, tags.GetFirstTag()));
        }

        /// <inheritdoc />
        public IMeter Meter(string name, string unitName, TimeUnits rateUnit, List<KeyValuePair<string, string>> tags = null)
        {
            return new Meter(_context.Meter(name, unitName, (TimeUnit)rateUnit, tags.GetFirstTag()));
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
        public IHistogram Histogram(string name, Units unit, SamplingTypes samplingType, List<KeyValuePair<string, string>> tags = null)
        {
            return new Histogram(_context.Histogram(name, unit.ToString(), (SamplingType)samplingType, tags.GetFirstTag()));
        }

        /// <inheritdoc />
        public ITimer Timer(string name, Units unit, SamplingTypes samplingType, TimeUnits rateUnit, TimeUnits durationUnit, List<KeyValuePair<string, string>> tags = null)
        {
            return new Timer(_context.Timer(name, unit.ToString(), (SamplingType)samplingType, (TimeUnit)rateUnit, (TimeUnit)durationUnit, tags.GetFirstTag()));
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
