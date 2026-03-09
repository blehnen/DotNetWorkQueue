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
    internal class TimerNet : ITimer, IDisposable
    {
        private readonly Histogram<double> _histogram;
        private readonly KeyValuePair<string, object>[] _tags;

        public TimerNet(Histogram<double> histogram, KeyValuePair<string, object>[] tags)
        {
            _histogram = histogram;
            _tags = tags;
        }

        public void Record(long time, TimeUnits unit, string userValue = null)
        {
            var milliseconds = ConvertToMilliseconds(time, unit);
            _histogram.Record(milliseconds, _tags);
        }

        public void Time(Action action, string userValue = null)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                action();
            }
            finally
            {
                sw.Stop();
                _histogram.Record(sw.Elapsed.TotalMilliseconds, _tags);
            }
        }

        public T Time<T>(Func<T> action, string userValue = null)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                return action();
            }
            finally
            {
                sw.Stop();
                _histogram.Record(sw.Elapsed.TotalMilliseconds, _tags);
            }
        }

        public ITimerContext NewContext(string userValue = null)
        {
            return new TimerContextNet(_histogram, _tags);
        }

        public void Dispose()
        {
        }

        private static double ConvertToMilliseconds(long time, TimeUnits unit)
        {
            switch (unit)
            {
                case TimeUnits.Nanoseconds: return time / 1_000_000.0;
                case TimeUnits.Microseconds: return time / 1_000.0;
                case TimeUnits.Milliseconds: return time;
                case TimeUnits.Seconds: return time * 1_000.0;
                case TimeUnits.Minutes: return time * 60_000.0;
                case TimeUnits.Hours: return time * 3_600_000.0;
                case TimeUnits.Days: return time * 86_400_000.0;
                default: return time;
            }
        }
    }
}
