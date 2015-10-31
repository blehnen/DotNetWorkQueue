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

namespace DotNetWorkQueue.Metrics.Net
{
    internal class Counter : ICounter
    {
        private readonly global::Metrics.Counter _counter;
        /// <summary>
        /// Initializes a new instance of the <see cref="Counter"/> class.
        /// </summary>
        /// <param name="counter">The counter.</param>
        public Counter(global::Metrics.Counter counter)
        {
            _counter = counter;
        }
        /// <summary>
        /// Increment the counter value.
        /// </summary>
        public void Increment()
        {
           _counter.Increment();
        }

        /// <summary>
        /// Increment the counter value for an item from a set.
        /// The counter value is incremented but the counter will also keep track and increment another counter associated with the <paramref name="item" />.
        /// The counter value will contain the total count and for each item the specific count and percentage of total count.
        /// </summary>
        /// <param name="item">Item from the set for which to increment the counter value.</param>
        public void Increment(string item)
        {
            _counter.Increment(item);
        }

        /// <summary>
        /// Increment the counter value with a specified amount.
        /// </summary>
        /// <param name="amount">The amount with which to increment the counter.</param>
        public void Increment(long amount)
        {
            _counter.Increment(amount);
        }

        /// <summary>
        /// Increment the counter value with a specified amount for an item from a set.
        /// The counter value is incremented but the counter will also keep track and increment another counter associated with the <paramref name="item" />.
        /// The counter value will contain the total count and for each item the specific count and percentage of total count.
        /// </summary>
        /// <param name="item">Item from the set for which to increment the counter value.</param>
        /// <param name="amount">The amount with which to increment the counter.</param>
        public void Increment(string item, long amount)
        {
            _counter.Increment(item, amount);
        }

        /// <summary>
        /// Decrement the counter value.
        /// </summary>
        public void Decrement()
        {
           _counter.Decrement();
        }

        /// <summary>
        /// Decrement the counter value for an item from a set.
        /// The counter value is decremented but the counter will also keep track and decrement another counter associated with the <paramref name="item" />.
        /// The counter value will contain the total count and for each item the specific count and percentage of total count.
        /// </summary>
        /// <param name="item">Item from the set for which to increment the counter value.</param>
        public void Decrement(string item)
        {
            _counter.Decrement(item);
        }

        /// <summary>
        /// Decrement the counter value with a specified amount.
        /// </summary>
        /// <param name="amount">The amount with which to increment the counter.</param>
        public void Decrement(long amount)
        {
           _counter.Decrement(amount);
        }

        /// <summary>
        /// Decrement the counter value with a specified amount for an item from a set.
        /// The counter value is decremented but the counter will also keep track and decrement another counter associated with the <paramref name="item" />.
        /// The counter value will contain the total count and for each item the specific count and percentage of total count.
        /// </summary>
        /// <param name="item">Item from the set for which to increment the counter value.</param>
        /// <param name="amount">The amount with which to increment the counter.</param>
        public void Decrement(string item, long amount)
        {
            _counter.Decrement(item, amount);
        }
    }
}
