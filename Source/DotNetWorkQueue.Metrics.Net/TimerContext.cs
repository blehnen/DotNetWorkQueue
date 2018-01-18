using System;

namespace DotNetWorkQueue.Metrics.Net
{
    /// <inheritdoc />
    internal class TimerContext: ITimerContext
    {
        private global::Metrics.TimerContext _timerContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerContext"/> class.
        /// </summary>
        /// <param name="timerContext">The timer context.</param>
        public TimerContext(global::Metrics.TimerContext timerContext)
        {
            _timerContext = timerContext;
        }

        /// <inheritdoc />
        public TimeSpan Elapsed => _timerContext.Elapsed;

        /// <inheritdoc />
        public void Dispose()
        {
            _timerContext.Dispose();
        }
    }
}
