// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
namespace DotNetWorkQueue.Metrics.Net
{
    /// <summary>
    /// A meter measures the rate at which a set of events occur, in a few different ways. 
    /// The mean rate is the average rate of events. It’s generally useful for trivia, 
    /// but as it represents the total rate for your application’s entire lifetime (e.g., the total number of requests handled, 
    /// divided by the number of seconds the process has been running), it does not offer a sense of recency. 
    /// Luckily, meters also record three different exponentially-weighted moving average rates: the 1-, 5-, and 15-minute moving averages.
    /// </summary>
    internal class Meter : IMeter
    {
        private readonly global::Metrics.Meter _meter;
        /// <summary>
        /// Initializes a new instance of the <see cref="Meter"/> class.
        /// </summary>
        /// <param name="meter">The meter.</param>
        public Meter(global::Metrics.Meter meter)
        {
            _meter = meter;
        }

        /// <summary>
        /// Mark the occurrence of an event.
        /// </summary>
        public void Mark()
        {
            _meter.Mark();
        }

        /// <summary>
        /// Mark the occurrence of an event for an item in a set.
        /// The total rate of the event is updated, but the meter will also keep track and update a specific rate for each <paramref name="item" /> registered.
        /// The meter value will contain the total rate and for each registered item the specific rate and percentage of total count.
        /// </summary>
        /// <param name="item">Item from the set for which to record the event.</param>
        public void Mark(string item)
        {
            _meter.Mark(item);
        }

        /// <summary>
        /// Mark the occurrence of <paramref name="count" /> events.
        /// </summary>
        /// <param name="count"></param>
        public void Mark(long count)
        {
            _meter.Mark(count);
        }

        /// <summary>
        /// Mark the occurrence of <paramref name="count" /> events for an item in a set.
        /// The total rate of the event is updated, but the meter will also keep track and update a specific rate for each <paramref name="item" /> registered.
        /// The meter value will contain the total rate and for each registered item the specific rate and percentage of total count.
        /// </summary>
        /// <param name="item">Item from the set for which to record the events.</param>
        /// <param name="count"></param>
        public void Mark(string item, long count)
        {
            _meter.Mark(item, count);
        }
    }
}
