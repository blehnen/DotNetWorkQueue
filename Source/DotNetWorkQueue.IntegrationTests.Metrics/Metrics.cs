using System;
using System.Collections.Concurrent;
using DotNetWorkQueue.Metrics.NoOp;

namespace DotNetWorkQueue.IntegrationTests.Metrics
{
    /// <inheritdoc cref="IMetrics" />
    public class Metrics: IMetrics, IDisposable
    {
        private readonly ConcurrentDictionary<string, Counter> _counters;
        private readonly ConcurrentDictionary<string, Meter> _meters;
        /// <summary>
        /// Initializes a new instance of the <see cref="Metrics"/> class.
        /// </summary>
        /// <param name="rootName">Name of the root.</param>
        public Metrics(string rootName)
        {
            _counters = new ConcurrentDictionary<string, Counter>();
            _meters = new ConcurrentDictionary<string, Meter>();
        }

        /// <inheritdoc />
        public void Gauge(string name, Func<double> valueProvider, Units unit, string tag = null)
        {
            //noop
        }

        /// <inheritdoc />
        public IMeter Meter(string name, Units unit, TimeUnits rateUnit, string tag = null)
        {
            if (_meters.ContainsKey(name))
                return _meters[name];
            _meters.TryAdd(name, new Meter());
            return _meters[name];
        }

        /// <inheritdoc />
        public IMeter Meter(string name, string unitName, TimeUnits rateUnit, string tag = null)
        {
            if (_meters.ContainsKey(name))
                return _meters[name];
            _meters.TryAdd(name, new Meter());
            return _meters[name];
        }

        /// <inheritdoc />
        public ICounter Counter(string name, Units unit, string tag = null)
        {
            if (_counters.ContainsKey(name))
                return _counters[name];
            _counters.TryAdd(name, new Counter());
            return _counters[name];
        }

        /// <inheritdoc />
        public ICounter Counter(string name, string unitName, string tag = null)
        {
            if (_counters.ContainsKey(name))
                return _counters[name];
            _counters.TryAdd(name, new Counter());
            return _counters[name];
        }

        /// <inheritdoc />
        public IHistogram Histogram(string name, Units unit, SamplingTypes samplingType, string tag = null)
        {
            return new HistogramNoOp();
        }

        /// <inheritdoc />
        public ITimer Timer(string name, Units unit, SamplingTypes samplingType, TimeUnits rateUnit, TimeUnits durationUnit, string tag = null)
        {
            return new TimerNoOp();
        }

        /// <inheritdoc />
        public IMetricsContext Context(string contextName)
        {
            return new MetricsContext();
        }

        /// <inheritdoc />
        public dynamic CollectedMetrics => new MetricsData(_meters, _counters);

        /// <inheritdoc />
        public void ShutdownContext(string contextName)
        {
            
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
               _counters.Clear();
            }
        }
    }
}
