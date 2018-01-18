using System.Threading;

namespace DotNetWorkQueue.IntegrationTests.Metrics
{
    /// <inheritdoc />
    public class Meter : IMeter
    {
        private long _counter;

        public long Value => Interlocked.Read(ref _counter);

        /// <inheritdoc />
        public void Mark()
        {
            Interlocked.Increment(ref _counter);
        }

        /// <inheritdoc />
        public void Mark(string item)
        {
            Interlocked.Increment(ref _counter);
        }

        /// <inheritdoc />
        public void Mark(long count)
        {
            Interlocked.Add(ref _counter, count);
        }

        /// <inheritdoc />
        public void Mark(string item, long count)
        {
            Interlocked.Add(ref _counter, count);
        }
    }
}
