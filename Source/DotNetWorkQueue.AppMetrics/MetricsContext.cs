// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using System.Linq;

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
            return new MetricsContext(new App.Metrics.MetricsBuilder()
                .Configuration.Configure(
                    options =>
                    {
                        options.DefaultContextLabel = contextName;
                        options.Enabled = true;
                        options.ReportingEnabled = true;
                    })
                .Build());
        }

        /// <inheritdoc />
        public void ShutdownContext(string contextName)
        {
            //noop
        }

        /// <inheritdoc />
        public void Gauge(string name, Func<double> valueProvider, Units unit, List<KeyValuePair<string, string>> tags = null)
        {
            var options = new App.Metrics.Gauge.GaugeOptions { Name = name, Tags = tags.GetTags(), MeasurementUnit = unit.GetUnit() };
            _context.Measure.Gauge.SetValue(options, valueProvider);
        }

        /// <inheritdoc />
        public ICounter Counter(string name, Units unit, List<KeyValuePair<string, string>> tags = null)
        {
            var options = new App.Metrics.Counter.CounterOptions { MeasurementUnit = unit.GetUnit(), Name = name, Tags = tags.GetTags() };
            return new Counter(_context.Provider.Counter.Instance(options));
        }

        /// <inheritdoc />
        public ICounter Counter(string name, string unitName, List<KeyValuePair<string, string>> tags = null)
        {
            var options = new App.Metrics.Counter.CounterOptions { MeasurementUnit = App.Metrics.Unit.Custom(unitName), Name = name, Tags = tags.GetTags() };
            return new Counter(_context.Provider.Counter.Instance(options));
        }

        /// <inheritdoc />
        public IMeter Meter(string name, Units unit, TimeUnits rateUnit = TimeUnits.Seconds, List<KeyValuePair<string, string>> tags = null)
        {
            var options = new App.Metrics.Meter.MeterOptions
            {
                Name = name,
                RateUnit = (App.Metrics.TimeUnit)rateUnit,
                MeasurementUnit = unit.GetUnit(),
                Tags = tags.GetTags()
            };
            return new Meter(_context.Provider.Meter.Instance(options));
        }

        /// <inheritdoc />
        public IMeter Meter(string name, string unitName, TimeUnits rateUnit, List<KeyValuePair<string, string>> tags = null)
        {
            var options = new App.Metrics.Meter.MeterOptions
            {
                Name = name,
                RateUnit = (App.Metrics.TimeUnit)rateUnit,
                MeasurementUnit = App.Metrics.Unit.Custom(unitName),
                Tags = tags.GetTags()
            };
            return new Meter(_context.Provider.Meter.Instance(options));
        }

        /// <inheritdoc />
        public IHistogram Histogram(string name, Units unit, SamplingTypes samplingType = SamplingTypes.FavorRecent, List<KeyValuePair<string, string>> tags = null)
        {
            var options = new App.Metrics.Histogram.HistogramOptions
            {
                MeasurementUnit = unit.GetUnit(),
                Name = name,
                Tags = tags.GetTags()
            };

            switch (samplingType)
            {
                case SamplingTypes.FavorRecent:
                    options.Reservoir = () => new App.Metrics.ReservoirSampling.ExponentialDecay.DefaultForwardDecayingReservoir(1024, 0.015, 0);
                    break;
                case SamplingTypes.LongTerm:
                    options.Reservoir = () => new App.Metrics.ReservoirSampling.Uniform.DefaultAlgorithmRReservoir(1024);
                    break;
                case SamplingTypes.SlidingWindow:
                    options.Reservoir = () => new App.Metrics.ReservoirSampling.SlidingWindow.DefaultSlidingWindowReservoir(1024);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(samplingType), samplingType, null);
            }
            return new Histogram(_context.Provider.Histogram.Instance(options));
        }

        /// <inheritdoc />
        public ITimer Timer(string name, Units unit, SamplingTypes samplingType = SamplingTypes.FavorRecent, TimeUnits rateUnit = TimeUnits.Seconds, TimeUnits durationUnit = TimeUnits.Milliseconds, List<KeyValuePair<string, string>> tags = null)
        {
            var options = new App.Metrics.Timer.TimerOptions
            {
                MeasurementUnit = unit.GetUnit(),
                Name = name,
                DurationUnit = (App.Metrics.TimeUnit)durationUnit,
                RateUnit = (App.Metrics.TimeUnit)rateUnit,
                Tags = tags.GetTags()
            };
            return new Timer(_context.Provider.Timer.Instance(options));
        }

        /// <inheritdoc />
        public void Dispose()
        {
           //noop
        }
    }
}
