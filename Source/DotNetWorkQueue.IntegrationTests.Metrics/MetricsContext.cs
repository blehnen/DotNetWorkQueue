// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
