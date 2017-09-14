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

using System.Threading;

namespace DotNetWorkQueue.IntegrationTests.Metrics
{
    public class Counter : ICounter
    {
        private long _counter;

        public long Value => Interlocked.Read(ref _counter);

        /// <inheritdoc />
        public void Increment()
        {
            Interlocked.Increment(ref _counter);
        }
        /// <inheritdoc />
        public void Increment(string item)
        {
            //item is ignored
            Interlocked.Increment(ref _counter);
        }

        /// <inheritdoc />
        public void Increment(long amount)
        {
            Interlocked.Add(ref _counter, amount);
        }

        /// <inheritdoc />
        public void Increment(string item, long amount)
        {
            //item is ignored
            Interlocked.Add(ref _counter, amount);
        }

        /// <inheritdoc />
        public void Decrement()
        {
            Interlocked.Decrement(ref _counter);
        }

        /// <inheritdoc />
        public void Decrement(string item)
        {
            //item is ignored
            Interlocked.Decrement(ref _counter);
        }

        /// <inheritdoc />
        public void Decrement(long amount)
        {
            Interlocked.Add(ref _counter, amount * -1);
        }

        /// <inheritdoc />
        public void Decrement(string item, long amount)
        {
            //item is ignored
            Interlocked.Add(ref _counter, amount * -1);
        }
    }
}
