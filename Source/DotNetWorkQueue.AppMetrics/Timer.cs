// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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

namespace DotNetWorkQueue.AppMetrics
{
    /// <inheritdoc />
    internal class Timer : ITimer
    {
        private readonly App.Metrics.Timer.ITimer _timer;
        /// <summary>
        /// Initializes a new instance of the <see cref="Timer"/> class.
        /// </summary>
        /// <param name="timer">The timer.</param>
        public Timer(App.Metrics.Timer.ITimer timer)
        {
            _timer = timer;
        }

        /// <inheritdoc />
        public void Record(long time, TimeUnits unit, string userValue = null)
        {
            _timer.Record(time, (App.Metrics.TimeUnit)unit, userValue);
        }

        /// <inheritdoc />
        public void Time(Action action, string userValue = null)
        {
            _timer.Time(action, userValue);
        }

        /// <inheritdoc />
        public T Time<T>(Func<T> action, string userValue = null)
        {
            return _timer.Time(action, userValue);
        }

        /// <inheritdoc />
        public ITimerContext NewContext(string userValue = null)
        {
            return new TimerContext(_timer.NewContext(userValue));
        }
    }
}
