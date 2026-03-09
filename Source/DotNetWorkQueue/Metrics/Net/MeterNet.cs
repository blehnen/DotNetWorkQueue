// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;

namespace DotNetWorkQueue.Metrics.Net
{
    internal class MeterNet : IMeter
    {
        private readonly Counter<long> _counter;
        private readonly KeyValuePair<string, object>[] _tags;
        private long _value;

        public MeterNet(Counter<long> counter, KeyValuePair<string, object>[] tags)
        {
            _counter = counter;
            _tags = tags;
        }

        public long Value => Interlocked.Read(ref _value);

        public void Mark()
        {
            Interlocked.Increment(ref _value);
            _counter.Add(1, _tags);
        }

        public void Mark(string item)
        {
            Interlocked.Increment(ref _value);
            _counter.Add(1, _tags);
        }

        public void Mark(long count)
        {
            Interlocked.Add(ref _value, count);
            _counter.Add(count, _tags);
        }

        public void Mark(string item, long count)
        {
            Interlocked.Add(ref _value, count);
            _counter.Add(count, _tags);
        }
    }
}
