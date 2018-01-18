using System;

namespace DotNetWorkQueue.IntegrationTests.Metrics
{
    /// <inheritdoc />
    internal class TimerContext: ITimerContext
    {
        /// <inheritdoc />
        public TimeSpan Elapsed => TimeSpan.MinValue;

        /// <inheritdoc />
        public void Dispose()
        {
            //noop
        }
    }
}
