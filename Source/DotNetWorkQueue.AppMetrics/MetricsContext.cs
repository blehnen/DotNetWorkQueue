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
    /// <inheritdoc />
    internal class MetricsContext : IMetricsContext
    {
        private readonly App.Metrics.IMetrics _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsContext"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public MetricsContext(App.Metrics.IMetrics context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public IMetricsContext Context(string contextName)
        {
            return MetricsStatic.Build(contextName);
        }

        /// <inheritdoc />
        public void ShutdownContext(string contextName)
        {
            //noop
        }

        /// <inheritdoc />
        public void Gauge(string name, Func<double> valueProvider, Units unit,
            List<KeyValuePair<string, string>> tags = null)
        {
            MetricsStatic.Gauge(_context, name, valueProvider, unit, tags);
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
        public IMeter Meter(string name, Units unit, TimeUnits rateUnit = TimeUnits.Seconds, List<KeyValuePair<string, string>> tags = null)
        {
            return MetricsStatic.Meter(_context, name, unit, rateUnit, tags);
        }

        /// <inheritdoc />
        public IMeter Meter(string name, string unitName, TimeUnits rateUnit, List<KeyValuePair<string, string>> tags = null)
        {
            return MetricsStatic.Meter(_context, name, unitName, rateUnit, tags);
        }

        /// <inheritdoc />
        public IHistogram Histogram(string name, Units unit, SamplingTypes samplingType = SamplingTypes.FavorRecent, List<KeyValuePair<string, string>> tags = null)
        {
            return MetricsStatic.Histogram(_context, name, unit, samplingType);
        }

        /// <inheritdoc />
        public ITimer Timer(string name, Units unit, SamplingTypes samplingType = SamplingTypes.FavorRecent, TimeUnits rateUnit = TimeUnits.Seconds, TimeUnits durationUnit = TimeUnits.Milliseconds, List<KeyValuePair<string, string>> tags = null)
        {
            return MetricsStatic.Timer(_context, name, unit, samplingType, rateUnit, durationUnit, tags);
        }

        /// <inheritdoc />
        public void Dispose()
        {
           //noop
        }
    }
}
