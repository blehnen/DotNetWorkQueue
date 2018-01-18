using System;

namespace DotNetWorkQueue.IntegrationTests.Metrics
{
    /// <inheritdoc />
    internal class Timer : ITimer
    {
        /// <inheritdoc />
        public void Record(long time, TimeUnits unit, string userValue = null)
        {
            //noop
        }

        /// <inheritdoc />
        public void Time(Action action, string userValue = null)
        {
            //noop
        }

        /// <inheritdoc />
        public T Time<T>(Func<T> action, string userValue = null)
        {
            //noop
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public ITimerContext NewContext(string userValue = null)
        {
            return new TimerContext();
        }
    }
}
