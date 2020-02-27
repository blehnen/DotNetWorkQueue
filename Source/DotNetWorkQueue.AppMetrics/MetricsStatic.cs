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
    internal static class MetricsStatic
    {
        public static IMetricsContext Build(string contextName)
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

        public static void Gauge(App.Metrics.IMetrics context, string name, Func<double> valueProvider, Units unit, List<KeyValuePair<string, string>> tags = null)
        {
            var options = new App.Metrics.Gauge.GaugeOptions { Name = name, Tags = tags.GetTags(), MeasurementUnit = unit.GetUnit() };
            context.Measure.Gauge.SetValue(options, valueProvider);
        }

        public static IMeter Meter(App.Metrics.IMetrics context, string name, Units unit, TimeUnits rateUnit, List<KeyValuePair<string, string>> tags = null)
        {
            var options = new App.Metrics.Meter.MeterOptions
            {
                Name = name,
                RateUnit = (App.Metrics.TimeUnit)rateUnit,
                MeasurementUnit = unit.GetUnit(),
                Tags = tags.GetTags()
            };
            return new Meter(context.Provider.Meter.Instance(options));
        }

        public static IMeter Meter(App.Metrics.IMetrics context, string name, string unitName, TimeUnits rateUnit, List<KeyValuePair<string, string>> tags = null)
        {
            var options = new App.Metrics.Meter.MeterOptions
            {
                Name = name,
                RateUnit = (App.Metrics.TimeUnit)rateUnit,
                MeasurementUnit = App.Metrics.Unit.Custom(unitName),
                Tags = tags.GetTags()
            };
            return new Meter(context.Provider.Meter.Instance(options));
        }

        public static ICounter Counter(App.Metrics.IMetrics context, string name, Units unit, List<KeyValuePair<string, string>> tags = null)
        {
            var options = new App.Metrics.Counter.CounterOptions { MeasurementUnit = unit.GetUnit(), Name = name, Tags = tags.GetTags() };
            return new Counter(context.Provider.Counter.Instance(options));
        }

        public static ICounter Counter(App.Metrics.IMetrics context, string name, string unitName, List<KeyValuePair<string, string>> tags = null)
        {
            var options = new App.Metrics.Counter.CounterOptions { MeasurementUnit = App.Metrics.Unit.Custom(unitName), Name = name, Tags = tags.GetTags() };
            return new Counter(context.Provider.Counter.Instance(options));
        }

        public static IHistogram Histogram(App.Metrics.IMetrics context, string name, Units unit, SamplingTypes samplingType, List<KeyValuePair<string, string>> tags = null)
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
            return new Histogram(context.Provider.Histogram.Instance(options));
        }

        public static ITimer Timer(App.Metrics.IMetrics context, string name, Units unit, SamplingTypes samplingType, TimeUnits rateUnit, TimeUnits durationUnit, List<KeyValuePair<string, string>> tags = null)
        {
            var options = new App.Metrics.Timer.TimerOptions
            {
                MeasurementUnit = unit.GetUnit(),
                Name = name,
                DurationUnit = (App.Metrics.TimeUnit)durationUnit,
                RateUnit = (App.Metrics.TimeUnit)rateUnit,
                Tags = tags.GetTags()
            };
            return new Timer(context.Provider.Timer.Instance(options));
        }
    }
}
