// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
using Metrics;
namespace DotNetWorkQueue.Metrics.Net
{
    /// <summary>
    /// Root metrics class; wraps metrics.net
    /// </summary>
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

        /// <summary>
        /// A gauge is the simplest metric type. It just returns a value. This metric is suitable for instantaneous values.
        /// </summary>
        /// <param name="name">Name of this gauge metric. Must be unique across all gauges in this context.</param>
        /// <param name="valueProvider">Function that returns the value for the gauge.</param>
        /// <param name="unit">Description of want the value represents ( Unit.Requests , Unit.Items etc ) .</param>
        /// <param name="tag">Optional tag that can be associated with the metric.</param>
        public void Gauge(string name, Func<double> valueProvider, Units unit, string tag = null)
        {
            _context.Gauge(name, valueProvider, unit.ToString(), tag);
        }

        /// <summary>
        /// A meter measures the rate at which a set of events occur, in a few different ways.
        /// This metric is suitable for keeping a record of now often something happens ( error, request etc ).
        /// </summary>
        /// <param name="name">Name of the metric. Must be unique across all meters in this context.</param>
        /// <param name="unit">Description of what the is being measured ( Unit.Requests , Unit.Items etc ) .</param>
        /// <param name="rateUnit">Time unit for rates reporting. Defaults to Second ( occurrences / second ).</param>
        /// <param name="tag">Optional tag that can be associated with the metric.</param>
        /// <returns>
        /// Reference to the metric
        /// </returns>
        /// <remarks>
        /// The mean rate is the average rate of events. It’s generally useful for trivia,
        /// but as it represents the total rate for your application’s entire lifetime (e.g., the total number of requests handled,
        /// divided by the number of seconds the process has been running), it does not offer a sense of recency.
        /// Luckily, meters also record three different exponentially-weighted moving average rates: the 1-, 5-, and 15-minute moving averages.
        /// </remarks>
        public IMeter Meter(string name, Units unit, TimeUnits rateUnit, string tag = null)
        {
            return new Meter(_context.Meter(name, unit.ToString(), (TimeUnit)rateUnit, tag));
        }

        /// <summary>
        /// A meter measures the rate at which a set of events occur, in a few different ways.
        /// This metric is suitable for keeping a record of now often something happens ( error, request etc ).
        /// </summary>
        /// <param name="name">Name of the metric. Must be unique across all meters in this context.</param>
        /// <param name="unitName">A Parent name; child counters can be added to this by specifying the this name</param>
        /// <param name="rateUnit">Time unit for rates reporting. Defaults to Second ( occurrences / second ).</param>
        /// <param name="tag">Optional tag that can be associated with the metric.</param>
        /// <returns>Reference to the metric</returns>
        /// <remarks>
        /// The mean rate is the average rate of events. It’s generally useful for trivia,
        /// but as it represents the total rate for your application’s entire lifetime (e.g., the total number of requests handled,
        /// divided by the number of seconds the process has been running), it does not offer a sense of recency.
        /// Luckily, meters also record three different exponentially-weighted moving average rates: the 1-, 5-, and 15-minute moving averages.
        /// </remarks>
        public IMeter Meter(string name, string unitName, TimeUnits rateUnit, string tag = null)
        {
            return new Meter(_context.Meter(name, unitName, (TimeUnit)rateUnit, tag));
        }

        /// <summary>
        /// A counter is a simple incrementing and decrementing 64-bit integer. Ex number of active requests.
        /// </summary>
        /// <param name="name">Name of the metric. Must be unique across all counters in this context.</param>
        /// <param name="unit">Description of what the is being measured ( Unit.Requests , Unit.Items etc ) .</param>
        /// <param name="tag">Optional tag that can be associated with the metric.</param>
        /// <returns></returns>
        public ICounter Counter(string name, Units unit, string tag = null)
        {
            return new Counter(_context.Counter(name, unit.ToString(), tag));
        }

        /// <summary>
        /// A counter is a simple incrementing and decrementing 64-bit integer. Ex number of active requests.
        /// </summary>
        /// <param name="name">Name of the metric. Must be unique across all counters in this context.</param>
        /// <param name="unitName">A Parent name; child counters can be added to this by specifying the this name</param>
        /// <param name="tag">Optional tag that can be associated with the metric.</param>
        /// <returns></returns>
        public ICounter Counter(string name, string unitName, string tag = null)
        {
            return new Counter(_context.Counter(name, unitName, tag));
        }

        /// <summary>
        /// A Histogram measures the distribution of values in a stream of data: e.g., the number of results returned by a search.
        /// </summary>
        /// <param name="name">Name of the metric. Must be unique across all histograms in this context.</param>
        /// <param name="unit">Description of what the is being measured ( Unit.Requests , Unit.Items etc ) .</param>
        /// <param name="samplingType">Type of the sampling to use (see SamplingType for details ).</param>
        /// <param name="tag">Optional tag that can be associated with the metric.</param>
        /// <returns></returns>
        public IHistogram Histogram(string name, Units unit, SamplingTypes samplingType, string tag = null)
        {
            return new Histogram(_context.Histogram(name, unit.ToString(), (SamplingType)samplingType, tag));
        }

        /// <summary>
        /// A timer is basically a histogram of the duration of a type of event and a meter of the rate of its occurrence.
        /// </summary>
        /// <param name="name">Name of the metric. Must be unique across all timers in this context.</param>
        /// <param name="unit">Description of what the is being measured ( Unit.Requests , Unit.Items etc ) .</param>
        /// <param name="samplingType">Type of the sampling to use (see SamplingType for details ).</param>
        /// <param name="rateUnit">Time unit for rates reporting. Defaults to Second ( occurrences / second ).</param>
        /// <param name="durationUnit">Time unit for reporting durations. Defaults to Milliseconds.</param>
        /// <param name="tag">Optional tag that can be associated with the metric.</param>
        /// <returns>
        /// Reference to the metric
        /// </returns>
        public ITimer Timer(string name, Units unit, SamplingTypes samplingType, TimeUnits rateUnit, TimeUnits durationUnit, string tag = null)
        {
            return new Timer(_context.Timer(name, unit.ToString(), (SamplingType)samplingType, (TimeUnit)rateUnit, (TimeUnit)durationUnit, tag));
        }

        /// <summary>
        /// Create a new child metrics context. Metrics added to the child context are kept separate from the metrics in the
        /// parent context.
        /// </summary>
        /// <param name="contextName">Name of the child context.</param>
        /// <returns>
        /// Newly created child context.
        /// </returns>
        public IMetricsContext Context(string contextName)
        {
            return new MetricsContext(_context.Context(contextName));
        }

        /// <summary>
        /// Gets the collected metrics.
        /// </summary>
        /// <value>
        /// The collected metrics.
        /// </value>
        public dynamic CollectedMetrics => _context.DataProvider.CurrentMetricsData;

        /// <summary>
        /// Remove a child context. The metrics for the child context are removed from the MetricsData of the parent context.
        /// </summary>
        /// <param name="contextName">Name of the child context to shutdown.</param>
        public void ShutdownContext(string contextName)
        {
            _context.ShutdownContext(contextName);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
