using System.Threading;

namespace DotNetWorkQueue.IntegrationTests.Metrics
{
    public class Counter : ICounter
    {
        private long _counter;

        public long Value => Interlocked.Read(ref _counter);

        /// <inheritdoc />
        public void Increment()
        {
            Interlocked.Increment(ref _counter);
        }
        /// <inheritdoc />
        public void Increment(string item)
        {
            //item is ignored
            Interlocked.Increment(ref _counter);
        }

        /// <inheritdoc />
        public void Increment(long amount)
        {
            Interlocked.Add(ref _counter, amount);
        }

        /// <inheritdoc />
        public void Increment(string item, long amount)
        {
            //item is ignored
            Interlocked.Add(ref _counter, amount);
        }

        /// <inheritdoc />
        public void Decrement()
        {
            Interlocked.Decrement(ref _counter);
        }

        /// <inheritdoc />
        public void Decrement(string item)
        {
            //item is ignored
            Interlocked.Decrement(ref _counter);
        }

        /// <inheritdoc />
        public void Decrement(long amount)
        {
            Interlocked.Add(ref _counter, amount * -1);
        }

        /// <inheritdoc />
        public void Decrement(string item, long amount)
        {
            //item is ignored
            Interlocked.Add(ref _counter, amount * -1);
        }
    }
}
