using System;
using Metrics;

namespace DotNetWorkQueue.Metrics.Net
{
    /// <inheritdoc />
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

        /// <inheritdoc />
        public void Record(long time, TimeUnits unit, string userValue = null)
        {
            _timer.Record(time, (TimeUnit)unit, userValue);
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
