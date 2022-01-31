// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
namespace DotNetWorkQueue.AppMetrics
{
    internal class Counter : ICounter
    {
        private readonly App.Metrics.Counter.ICounter _counter;
        /// <summary>
        /// Initializes a new instance of the <see cref="Counter"/> class.
        /// </summary>
        /// <param name="counter">The counter.</param>
        public Counter(App.Metrics.Counter.ICounter counter)
        {
            _counter = counter;
        }
        /// <inheritdoc />
        public void Increment()
        {
            _counter.Increment();
        }
        /// <inheritdoc />
        public void Increment(string item)
        {
            _counter.Increment(item);
        }

        /// <inheritdoc />
        public void Increment(long amount)
        {
            _counter.Increment(amount);
        }

        /// <inheritdoc />
        public void Increment(string item, long amount)
        {
            _counter.Increment(item, amount);
        }

        /// <inheritdoc />
        public void Decrement()
        {
            _counter.Decrement();
        }

        /// <inheritdoc />
        public void Decrement(string item)
        {
            _counter.Decrement(item);
        }

        /// <inheritdoc />
        public void Decrement(long amount)
        {
            _counter.Decrement(amount);
        }

        /// <inheritdoc />
        public void Decrement(string item, long amount)
        {
            _counter.Decrement(item, amount);
        }
    }
}
