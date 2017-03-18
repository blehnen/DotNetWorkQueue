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
using System;
using Metrics;
namespace DotNetWorkQueue.Metrics.Net
{
    internal class Timer : ITimer
    {
        private readonly global::Metrics.Timer _timer;
        /// <summary>
        /// Initializes a new instance of the <see cref="Timer"/> class.
        /// </summary>
        /// <param name="timer">The timer.</param>
        public Timer(global::Metrics.Timer timer)
        {
            _timer = timer;
        }

        /// <summary>
        /// Manually record timer value
        /// </summary>
        /// <param name="time">The value representing the manually measured time.</param>
        /// <param name="unit">Unit for the value.</param>
        /// <param name="userValue">A custom user value that will be associated to the results.
        /// Useful for tracking (for example) for which id the max or min value was recorded.</param>
        public void Record(long time, TimeUnits unit, string userValue = null)
        {
            _timer.Record(time, (TimeUnit)unit, userValue);
        }

        /// <summary>
        /// Runs the <paramref name="action" /> and records the time it took.
        /// </summary>
        /// <param name="action">Action to run and record time for.</param>
        /// <param name="userValue">A custom user value that will be associated to the results.
        /// Useful for tracking (for example) for which id the max or min value was recorded.</param>
        public void Time(Action action, string userValue = null)
        {
            _timer.Time(action, userValue);
        }

        /// <summary>
        /// Runs the <paramref name="action" /> returning the result and records the time it took.
        /// </summary>
        /// <typeparam name="T">Type of the value returned by the action</typeparam>
        /// <param name="action">Action to run and record time for.</param>
        /// <param name="userValue">A custom user value that will be associated to the results.
        /// Useful for tracking (for example) for which id the max or min value was recorded.</param>
        /// <returns>
        /// The result of the <paramref name="action" />
        /// </returns>
        public T Time<T>(Func<T> action, string userValue = null)
        {
            return _timer.Time(action, userValue);
        }

        /// <summary>
        /// Creates a new disposable instance and records the time it takes until the instance is disposed.
        /// <code>
        /// using(timer.NewContext())
        /// {
        /// ExecuteMethodThatNeedsMonitoring();
        /// }
        /// </code>
        /// </summary>
        /// <param name="userValue">A custom user value that will be associated to the results.
        /// Useful for tracking (for example) for which id the max or min value was recorded.</param>
        /// <returns>
        /// A disposable instance that will record the time passed until disposed.
        /// </returns>
        public ITimerContext NewContext(string userValue = null)
        {
            return new TimerContext(_timer.NewContext(userValue));
        }
    }
}
