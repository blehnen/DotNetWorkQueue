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

namespace DotNetWorkQueue
{
    /// <summary>
    /// Capture metrics for the queue and/or user code
    /// </summary>
    public interface IMetrics
    {
        /// <summary>
        /// Create a new child metrics context. Metrics added to the child context are kept separate from the metrics in the 
        /// parent context.
        /// </summary>
        /// <param name="contextName">Name of the child context.</param>
        /// <returns>Newly created child context.</returns>
        IMetricsContext Context(string contextName);

        /// <summary>
        /// Remove a child context. The metrics for the child context are removed from the MetricsData of the parent context.
        /// </summary>
        /// <param name="contextName">Name of the child context to shutdown.</param>
        void ShutdownContext(string contextName);

        /// <summary>
        /// A gauge is the simplest metric type. It just returns a value. This metric is suitable for instantaneous values.
        /// </summary>
        /// <param name="name">Name of this gauge metric. Must be unique across all gauges in this context.</param>
        /// <param name="valueProvider">Function that returns the value for the gauge.</param>
        /// <param name="unit">Description of want the value represents ( Unit.Requests , Unit.Items etc ) .</param>
        /// <param name="tags">Optional tags that can be associated with the metric.</param>
        void Gauge(string name, Func<double> valueProvider, Units unit, List<KeyValuePair<string, string>> tags = null);

        /// <summary>
        /// A counter is a simple incrementing and decrementing 64-bit integer. Ex number of active requests.
        /// </summary>
        /// <param name="name">Name of the metric. Must be unique across all counters in this context.</param>
        /// <param name="unit">Description of what the is being measured ( Unit.Requests , Unit.Items etc ) .</param>
        /// <param name="tags">Optional tags that can be associated with the metric.</param>
        /// <returns>Reference to the metric</returns>
        ICounter Counter(string name, Units unit, List<KeyValuePair<string, string>> tags = null);

        /// <summary>
        /// A counter is a simple incrementing and decrementing 64-bit integer. Ex number of active requests.
        /// </summary>
        /// <param name="name">Name of the metric. Must be unique across all counters in this context.</param>
        /// <param name="unitName">A Parent name; child counters can be added to this by specifying the this name</param>
        /// <param name="tags">Optional tags that can be associated with the metric.</param>
        /// <returns></returns>
        ICounter Counter(string name, string unitName, List<KeyValuePair<string, string>> tags = null);

        /// <summary>
        /// A meter measures the rate at which a set of events occur, in a few different ways. 
        /// This metric is suitable for keeping a record of now often something happens ( error, request etc ).
        /// </summary>
        /// <remarks>
        /// The mean rate is the average rate of events. It’s generally useful for trivia, 
        /// but as it represents the total rate for your application’s entire lifetime (e.g., the total number of requests handled, 
        /// divided by the number of seconds the process has been running), it does not offer a sense of recency. 
        /// Luckily, meters also record three different exponentially-weighted moving average rates: the 1-, 5-, and 15-minute moving averages.
        /// </remarks>
        /// <param name="name">Name of the metric. Must be unique across all meters in this context.</param>
        /// <param name="unit">Description of what the is being measured ( Unit.Requests , Unit.Items etc ) .</param>
        /// <param name="rateUnit">Time unit for rates reporting. Defaults to Second ( occurrences / second ).</param>
        /// <param name="tags">Optional tags that can be associated with the metric.</param>
        /// <returns>Reference to the metric</returns>
        IMeter Meter(string name, Units unit, TimeUnits rateUnit = TimeUnits.Seconds, List<KeyValuePair<string, string>> tags = null);

        /// <summary>
        /// A meter measures the rate at which a set of events occur, in a few different ways.
        /// This metric is suitable for keeping a record of now often something happens ( error, request etc ).
        /// </summary>
        /// <param name="name">Name of the metric. Must be unique across all meters in this context.</param>
        /// <param name="unitName">A Parent name; child counters can be added to this by specifying the this name</param>
        /// <param name="rateUnit">Time unit for rates reporting. Defaults to Second ( occurrences / second ).</param>
        /// <param name="tags">Optional tags that can be associated with the metric.</param>
        /// <returns>Reference to the metric</returns>
        /// <remarks>
        /// The mean rate is the average rate of events. It’s generally useful for trivia,
        /// but as it represents the total rate for your application’s entire lifetime (e.g., the total number of requests handled,
        /// divided by the number of seconds the process has been running), it does not offer a sense of recency.
        /// Luckily, meters also record three different exponentially-weighted moving average rates: the 1-, 5-, and 15-minute moving averages.
        /// </remarks>
        IMeter Meter(string name, string unitName, TimeUnits rateUnit, List<KeyValuePair<string, string>> tags = null);

        /// <summary>
        /// A Histogram measures the distribution of values in a stream of data: e.g., the number of results returned by a search.
        /// </summary>
        /// <param name="name">Name of the metric. Must be unique across all histograms in this context.</param>
        /// <param name="unit">Description of what the is being measured ( Unit.Requests , Unit.Items etc ) .</param>
        /// <param name="samplingType">Type of the sampling to use (see SamplingType for details ).</param>
        /// <param name="tags">Optional tags that can be associated with the metric.</param>
        /// <returns>Reference to the metric</returns>
        IHistogram Histogram(string name,
            Units unit,
            SamplingTypes samplingType = SamplingTypes.FavorRecent,
            List<KeyValuePair<string, string>> tags = null);

        /// <summary>
        /// A timer is basically a histogram of the duration of a type of event and a meter of the rate of its occurrence.
        /// </summary>
        /// <param name="name">Name of the metric. Must be unique across all timers in this context.</param>
        /// <param name="unit">Description of what the is being measured ( Unit.Requests , Unit.Items etc ) .</param>
        /// <param name="samplingType">Type of the sampling to use (see SamplingType for details ).</param>
        /// <param name="rateUnit">Time unit for rates reporting. Defaults to Second ( occurrences / second ).</param>
        /// <param name="durationUnit">Time unit for reporting durations. Defaults to Milliseconds. </param>
        /// <param name="tags">Optional tags that can be associated with the metric.</param>
        /// <returns>Reference to the metric</returns>
        ITimer Timer(string name,
            Units unit,
            SamplingTypes samplingType = SamplingTypes.FavorRecent,
            TimeUnits rateUnit = TimeUnits.Seconds,
            TimeUnits durationUnit = TimeUnits.Milliseconds,
            List<KeyValuePair<string, string>> tags = null);

        /// <summary>
        /// Gets the collected metrics.
        /// </summary>
        /// <value>
        /// The collected metrics.
        /// </value>
        dynamic CollectedMetrics { get; }
    }

    /// <summary>
    /// Represents a logical grouping of metrics
    /// </summary>
    public interface IMetricsContext : IDisposable
    {
        /// <summary>
        /// Create a new child metrics context. Metrics added to the child context are kept separate from the metrics in the 
        /// parent context.
        /// </summary>
        /// <param name="contextName">Name of the child context.</param>
        /// <returns>Newly created child context.</returns>
        IMetricsContext Context(string contextName);

        /// <summary>
        /// Remove a child context. The metrics for the child context are removed from the MetricsData of the parent context.
        /// </summary>
        /// <param name="contextName">Name of the child context to shutdown.</param>
        void ShutdownContext(string contextName);

        /// <summary>
        /// A gauge is the simplest metric type. It just returns a value. This metric is suitable for instantaneous values.
        /// </summary>
        /// <param name="name">Name of this gauge metric. Must be unique across all gauges in this context.</param>
        /// <param name="valueProvider">Function that returns the value for the gauge.</param>
        /// <param name="unit">Description of want the value represents ( Unit.Requests , Unit.Items etc ) .</param>
        /// <param name="tags">Optional tags that can be associated with the metric.</param>
        void Gauge(string name, Func<double> valueProvider, Units unit, List<KeyValuePair<string, string>> tags = null);

        /// <summary>
        /// A counter is a simple incrementing and decrementing 64-bit integer. Ex number of active requests.
        /// </summary>
        /// <param name="name">Name of the metric. Must be unique across all counters in this context.</param>
        /// <param name="unit">Description of what the is being measured ( Unit.Requests , Unit.Items etc ) .</param>
        /// <param name="tags">Optional tags that can be associated with the metric.</param>
        /// <returns>Reference to the metric</returns>
        ICounter Counter(string name, Units unit, List<KeyValuePair<string, string>> tags = null);

        /// <summary>
        /// A counter is a simple incrementing and decrementing 64-bit integer. Ex number of active requests.
        /// </summary>
        /// <param name="name">Name of the metric. Must be unique across all counters in this context.</param>
        /// <param name="unitName">A Parent name; child counters can be added to this by specifying the this name</param>
        /// <param name="tags">Optional tags that can be associated with the metric.</param>
        /// <returns></returns>
        ICounter Counter(string name, string unitName, List<KeyValuePair<string, string>> tags = null);

        /// <summary>
        /// A meter measures the rate at which a set of events occur, in a few different ways. 
        /// This metric is suitable for keeping a record of now often something happens ( error, request etc ).
        /// </summary>
        /// <remarks>
        /// The mean rate is the average rate of events. It’s generally useful for trivia, 
        /// but as it represents the total rate for your application’s entire lifetime (e.g., the total number of requests handled, 
        /// divided by the number of seconds the process has been running), it does not offer a sense of recency. 
        /// Luckily, meters also record three different exponentially-weighted moving average rates: the 1-, 5-, and 15-minute moving averages.
        /// </remarks>
        /// <param name="name">Name of the metric. Must be unique across all meters in this context.</param>
        /// <param name="unit">Description of what the is being measured ( Unit.Requests , Unit.Items etc ) .</param>
        /// <param name="rateUnit">Time unit for rates reporting. Defaults to Second ( occurrences / second ).</param>
        /// <param name="tags">Optional tags that can be associated with the metric.</param>
        /// <returns>Reference to the metric</returns>
        IMeter Meter(string name, Units unit, TimeUnits rateUnit = TimeUnits.Seconds, List<KeyValuePair<string, string>> tags = null);

        /// <summary>
        /// A meter measures the rate at which a set of events occur, in a few different ways.
        /// This metric is suitable for keeping a record of now often something happens ( error, request etc ).
        /// </summary>
        /// <param name="name">Name of the metric. Must be unique across all meters in this context.</param>
        /// <param name="unitName">A Parent name; child counters can be added to this by specifying the this name</param>
        /// <param name="rateUnit">Time unit for rates reporting. Defaults to Second ( occurrences / second ).</param>
        /// <param name="tags">Optional tags that can be associated with the metric.</param>
        /// <returns>Reference to the metric</returns>
        /// <remarks>
        /// The mean rate is the average rate of events. It’s generally useful for trivia,
        /// but as it represents the total rate for your application’s entire lifetime (e.g., the total number of requests handled,
        /// divided by the number of seconds the process has been running), it does not offer a sense of recency.
        /// Luckily, meters also record three different exponentially-weighted moving average rates: the 1-, 5-, and 15-minute moving averages.
        /// </remarks>
        IMeter Meter(string name, string unitName, TimeUnits rateUnit, List<KeyValuePair<string, string>> tags = null);

        /// <summary>
        /// A Histogram measures the distribution of values in a stream of data: e.g., the number of results returned by a search.
        /// </summary>
        /// <param name="name">Name of the metric. Must be unique across all histograms in this context.</param>
        /// <param name="unit">Description of what the is being measured ( Unit.Requests , Unit.Items etc ) .</param>
        /// <param name="samplingType">Type of the sampling to use (see SamplingType for details ).</param>
        /// <param name="tags">Optional tags that can be associated with the metric.</param>
        /// <returns>Reference to the metric</returns>
        IHistogram Histogram(string name,
            Units unit,
            SamplingTypes samplingType = SamplingTypes.FavorRecent,
            List<KeyValuePair<string, string>> tags = null);

        /// <summary>
        /// A timer is basically a histogram of the duration of a type of event and a meter of the rate of its occurrence.
        /// </summary>
        /// <param name="name">Name of the metric. Must be unique across all timers in this context.</param>
        /// <param name="unit">Description of what the is being measured ( Unit.Requests , Unit.Items etc ) .</param>
        /// <param name="samplingType">Type of the sampling to use (see SamplingType for details ).</param>
        /// <param name="rateUnit">Time unit for rates reporting. Defaults to Second ( occurrences / second ).</param>
        /// <param name="durationUnit">Time unit for reporting durations. Defaults to Milliseconds. </param>
        /// <param name="tags">Optional tags that can be associated with the metric.</param>
        /// <returns>Reference to the metric</returns>
        ITimer Timer(string name,
            Units unit,
            SamplingTypes samplingType = SamplingTypes.FavorRecent,
            TimeUnits rateUnit = TimeUnits.Seconds,
            TimeUnits durationUnit = TimeUnits.Milliseconds,
            List<KeyValuePair<string, string>> tags = null);
    }
    /// <summary>
    /// A timer is basically a histogram of the duration of a type of event and a meter of the rate of its occurrence.
    /// <seealso cref="IHistogram"/> and <seealso cref="IMeter"/>
    /// </summary>
    public interface ITimer
    {
        /// <summary>
        /// Manually record timer value
        /// </summary>
        /// <param name="time">The value representing the manually measured time.</param>
        /// <param name="unit">Unit for the value.</param>
        /// <param name="userValue">A custom user value that will be associated to the results.
        /// Useful for tracking (for example) for which id the max or min value was recorded.
        /// </param>
        void Record(long time, TimeUnits unit, string userValue = null);

        /// <summary>
        /// Runs the <paramref name="action"/> and records the time it took.
        /// </summary>
        /// <param name="action">Action to run and record time for.</param>
        /// <param name="userValue">A custom user value that will be associated to the results.
        /// Useful for tracking (for example) for which id the max or min value was recorded.
        /// </param>
        void Time(Action action, string userValue = null);

        /// <summary>
        /// Runs the <paramref name="action"/> returning the result and records the time it took.
        /// </summary>
        /// <typeparam name="T">Type of the value returned by the action</typeparam>
        /// <param name="action">Action to run and record time for.</param>
        /// <param name="userValue">A custom user value that will be associated to the results.
        /// Useful for tracking (for example) for which id the max or min value was recorded.
        /// </param>
        /// <returns>The result of the <paramref name="action"/></returns>
        T Time<T>(Func<T> action, string userValue = null);

        /// <summary>
        /// Creates a new disposable instance and records the time it takes until the instance is disposed.
        /// <code>
        /// using(timer.NewContext())
        /// {
        ///     ExecuteMethodThatNeedsMonitoring();
        /// }
        /// </code>
        /// </summary>
        /// <param name="userValue">A custom user value that will be associated to the results.
        /// Useful for tracking (for example) for which id the max or min value was recorded.
        /// </param>
        /// <returns>A disposable instance that will record the time passed until disposed.</returns>
        ITimerContext NewContext(string userValue = null);
    }

    /// <summary>
    /// Disposable instance used to measure time. 
    /// </summary>
    public interface ITimerContext : IDisposable
    {
        /// <summary>
        /// Provides the currently elapsed time from when the instance has been created
        /// </summary>
        TimeSpan Elapsed { get; }
    }

    /// <summary>
    /// A Histogram measures the distribution of values in a stream of data: e.g., the number of results returned by a search.
    /// </summary>
    public interface IHistogram 
    {
        /// <summary>
        /// Records a value.
        /// </summary>
        /// <param name="value">Value to be added to the histogram.</param>
        /// <param name="userValue">A custom user value that will be associated to the results.
        /// Useful for tracking (for example) for which id the max or min value was recorded.
        /// </param>
        void Update(long value, string userValue = null);
    }

    /// <summary>
    /// A counter is a simple incrementing and decrementing 64-bit integer.
    /// Each operation can also be applied to a item from a set and the counter will store individual count for each set item.
    /// </summary>
    public interface ICounter
    {
        /// <summary>
        /// Increment the counter value.
        /// </summary>
        void Increment();

        /// <summary>
        /// Increment the counter value for an item from a set.
        /// The counter value is incremented but the counter will also keep track and increment another counter associated with the <paramref name="item"/>.
        /// The counter value will contain the total count and for each item the specific count and percentage of total count.
        /// </summary>
        /// <param name="item">Item from the set for which to increment the counter value.</param>
        void Increment(string item);

        /// <summary>
        /// Increment the counter value with a specified amount.
        /// </summary>
        /// <param name="amount">The amount with which to increment the counter.</param>
        void Increment(long amount);

        /// <summary>
        /// Increment the counter value with a specified amount for an item from a set.
        /// The counter value is incremented but the counter will also keep track and increment another counter associated with the <paramref name="item"/>.
        /// The counter value will contain the total count and for each item the specific count and percentage of total count.
        /// </summary>
        /// <param name="item">Item from the set for which to increment the counter value.</param>
        /// <param name="amount">The amount with which to increment the counter.</param>
        void Increment(string item, long amount);

        /// <summary>
        /// Decrement the counter value.
        /// </summary>
        void Decrement();

        /// <summary>
        /// Decrement the counter value for an item from a set.
        /// The counter value is decremented but the counter will also keep track and decrement another counter associated with the <paramref name="item"/>.
        /// The counter value will contain the total count and for each item the specific count and percentage of total count.
        /// </summary>
        /// <param name="item">Item from the set for which to increment the counter value.</param>
        void Decrement(string item);

        /// <summary>
        /// Decrement the counter value with a specified amount.
        /// </summary>
        /// <param name="amount">The amount with which to increment the counter.</param>
        void Decrement(long amount);

        /// <summary>
        /// Decrement the counter value with a specified amount for an item from a set.
        /// The counter value is decremented but the counter will also keep track and decrement another counter associated with the <paramref name="item"/>.
        /// The counter value will contain the total count and for each item the specific count and percentage of total count.
        /// </summary>
        /// <param name="item">Item from the set for which to increment the counter value.</param>
        /// <param name="amount">The amount with which to increment the counter.</param>
        void Decrement(string item, long amount);
    }

    /// <summary>
    /// A meter measures the rate at which a set of events occur, in a few different ways. 
    /// The mean rate is the average rate of events. It’s generally useful for trivia, 
    /// but as it represents the total rate for your application’s entire lifetime (e.g., the total number of requests handled, 
    /// divided by the number of seconds the process has been running), it does not offer a sense of recency. 
    /// Luckily, meters also record three different exponentially-weighted moving average rates: the 1-, 5-, and 15-minute moving averages.
    /// </summary>
    public interface IMeter
    {
        /// <summary>
        /// Mark the occurrence of an event.
        /// </summary>
        void Mark();

        /// <summary>
        /// Mark the occurrence of an event for an item in a set.
        /// The total rate of the event is updated, but the meter will also keep track and update a specific rate for each <paramref name="item"/> registered.
        /// The meter value will contain the total rate and for each registered item the specific rate and percentage of total count.
        /// </summary>
        /// <param name="item">Item from the set for which to record the event.</param>
        void Mark(string item);

        /// <summary>
        /// Mark the occurrence of <paramref name="count"/> events.
        /// </summary>
        /// <param name="count"></param>
        void Mark(long count);

        /// <summary>
        /// Mark the occurrence of <paramref name="count"/> events for an item in a set.
        /// The total rate of the event is updated, but the meter will also keep track and update a specific rate for each <paramref name="item"/> registered.
        /// The meter value will contain the total rate and for each registered item the specific rate and percentage of total count.
        /// </summary>
        /// <param name="count"></param>
        /// <param name="item">Item from the set for which to record the events.</param>
        void Mark(string item, long count);
    }

    /// <summary>
    /// Type of the sampling to use
    /// </summary>
    public enum SamplingTypes
    {   
        /// <summary>
        /// Sampling will be done with a Exponentially Decaying Reservoir
        /// </summary>
        FavorRecent = 0,      
        /// <summary>
        /// Sampling will done with a Uniform Reservoir.
        /// </summary>
        LongTerm = 1,
        /// <summary>
        ///  Sampling will done with a Sliding Window Reservoir.  A histogram with a sliding
        ///  window reservoir produces quantiles which are representative of the past
        ///  N measurements.
        /// </summary>
        SlidingWindow = 2
    }

    /// <summary>
    /// Unit of time to use
    /// </summary>
    public enum TimeUnits
    {
        /// <summary>
        /// nanoseconds
        /// </summary>
        Nanoseconds = 0,
        /// <summary>
        /// microseconds
        /// </summary>
        Microseconds = 1,
        /// <summary>
        /// milliseconds
        /// </summary>
        Milliseconds = 2,
        /// <summary>
        /// seconds
        /// </summary>
        Seconds = 3,
        /// <summary>
        /// minutes
        /// </summary>
        Minutes = 4,
        /// <summary>
        /// hours
        /// </summary>
        Hours = 5,
        /// <summary>
        /// days
        /// </summary>
        Days = 6
    }
    /// <summary>
    /// Unit of Measure
    /// </summary>
    public enum Units
    {
        /// <summary>
        /// The bytes
        /// </summary>
        Bytes = 0,
        /// <summary>
        /// The calls
        /// </summary>
        Calls = 1,
        /// <summary>
        /// The commands
        /// </summary>
        Commands = 2,
        /// <summary>
        /// The errors
        /// </summary>
        Errors = 3,
        /// <summary>
        /// The events
        /// </summary>
        Events = 4,
        /// <summary>
        /// The items
        /// </summary>
        Items = 5,
        /// <summary>
        /// The kilo bytes
        /// </summary>
        KiloBytes = 6,
        /// <summary>
        /// The mega bytes
        /// </summary>
        MegaBytes = 7,
        /// <summary>
        /// The none
        /// </summary>
        None = 8,
        /// <summary>
        /// The percent
        /// </summary>
        Percent = 9,
        /// <summary>
        /// The requests
        /// </summary>
        Requests = 10,
        /// <summary>
        /// The results
        /// </summary>
        Results = 11,
        /// <summary>
        /// The threads
        /// </summary>
        Threads = 12
    }
}
