// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
namespace DotNetWorkQueue.AppMetrics
{
    /// <inheritdoc cref="IMetrics" />
    public class Metrics: IMetrics, IDisposable
    {
        private readonly App.Metrics.IMetrics  _context;
        /// <summary>
        /// Initializes a new instance of the <see cref="Metrics"/> class.
        /// </summary>
        /// <param name="rootName">Name of the root.</param>
        public Metrics(string rootName)
        {
            _context = new App.Metrics.MetricsBuilder()
                .Configuration.Configure(
                    options =>
                    {
                        options.DefaultContextLabel = rootName;
                        options.Enabled = true;
                        options.ReportingEnabled = true;
                    })
                .Build();
        }

        /// <summary>Initializes a new instance of the <see cref="Metrics"/> class.</summary>
        /// <param name="metrics">The metrics.</param>
        public Metrics(App.Metrics.IMetrics metrics)
        {
            _context = metrics;
        }

        /// <inheritdoc />
        public void Gauge(string name, Func<double> valueProvider, Units unit, List<KeyValuePair<string, string>> tags = null)
        {
            MetricsStatic.Gauge(_context, name, valueProvider, unit, tags);
        }

        /// <inheritdoc />
        public IMeter Meter(string name, Units unit, TimeUnits rateUnit, List<KeyValuePair<string, string>> tags = null)
        {
            return MetricsStatic.Meter(_context, name, unit, rateUnit, tags);
        }

        /// <inheritdoc />
        public IMeter Meter(string name, string unitName, TimeUnits rateUnit, List<KeyValuePair<string, string>> tags = null)
        {
            return MetricsStatic.Meter(_context, name, unitName, rateUnit, tags);
        }

        /// <inheritdoc />
        public ICounter Counter(string name, Units unit, List<KeyValuePair<string, string>> tags = null)
        {
            return MetricsStatic.Counter(_context, name, unit, tags);
        }

        /// <inheritdoc />
        public ICounter Counter(string name, string unitName, List<KeyValuePair<string, string>> tags = null)
        {
            return MetricsStatic.Counter(_context, name, unitName, tags);
        }

        /// <inheritdoc />
        public IHistogram Histogram(string name, Units unit, SamplingTypes samplingType, List<KeyValuePair<string, string>> tags = null)
        {
            return MetricsStatic.Histogram(_context, name, unit, samplingType, tags);
        }

        /// <inheritdoc />
        public ITimer Timer(string name, Units unit, SamplingTypes samplingType, TimeUnits rateUnit, TimeUnits durationUnit, List<KeyValuePair<string, string>> tags = null)
        {
            return MetricsStatic.Timer(_context, name, unit, samplingType, rateUnit, durationUnit, tags);
        }

        /// <inheritdoc />
        public IMetricsContext Context(string contextName)
        {
            return MetricsStatic.Build(contextName);
        }

        /// <inheritdoc />
        public dynamic CollectedMetrics => _context.Snapshot.Get();

        /// <inheritdoc />
        public void ShutdownContext(string contextName)
        {
            //noop
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
            //noop
        }
    }
}
