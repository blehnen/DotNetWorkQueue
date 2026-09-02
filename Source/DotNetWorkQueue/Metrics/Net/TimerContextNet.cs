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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace DotNetWorkQueue.Metrics.Net
{
    /// <remarks>
    /// The elapsed time is kept as a raw timestamp rather than a <see cref="Stopwatch"/>. One of
    /// these is created for every message sent and every message consumed, and a
    /// <see cref="Stopwatch"/> is a class - it was a second allocation per operation for a value
    /// that fits in a <see cref="long"/>.
    /// </remarks>
    internal sealed class TimerContextNet : ITimerContext
    {
        private readonly Histogram<double> _histogram;
        private readonly KeyValuePair<string, object>[] _tags;
        private readonly long _start;

        public TimerContextNet(Histogram<double> histogram, KeyValuePair<string, object>[] tags)
        {
            _histogram = histogram;
            _tags = tags;
            _start = Stopwatch.GetTimestamp();
        }

        public TimeSpan Elapsed => Stopwatch.GetElapsedTime(_start);

        public void Dispose()
        {
            _histogram.Record(Stopwatch.GetElapsedTime(_start).TotalMilliseconds, _tags);
        }
    }
}
