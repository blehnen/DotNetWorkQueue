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
    internal class TimerContextNet : ITimerContext
    {
        private readonly Histogram<double> _histogram;
        private readonly KeyValuePair<string, object>[] _tags;
        private readonly Stopwatch _stopwatch;

        public TimerContextNet(Histogram<double> histogram, KeyValuePair<string, object>[] tags)
        {
            _histogram = histogram;
            _tags = tags;
            _stopwatch = Stopwatch.StartNew();
        }

        public TimeSpan Elapsed => _stopwatch.Elapsed;

        public void Dispose()
        {
            _stopwatch.Stop();
            _histogram.Record(_stopwatch.Elapsed.TotalMilliseconds, _tags);
        }
    }
}
