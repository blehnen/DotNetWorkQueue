namespace DotNetWorkQueue.Metrics.Net
{
    internal class Counter : ICounter
    {
        private readonly global::Metrics.Counter _counter;
        /// <summary>
        /// Initializes a new instance of the <see cref="Counter"/> class.
        /// </summary>
        /// <param name="counter">The counter.</param>
        public Counter(global::Metrics.Counter counter)
        {
            _counter = counter;
        }
        /// <inheritdoc />
        public void Increment()
        {
           _counter.Increment();
        }
        /// <inheritdoc />
        public void Increment(string item)
        {
            _counter.Increment(item);
        }

        /// <inheritdoc />
        public void Increment(long amount)
        {
            _counter.Increment(amount);
        }

        /// <inheritdoc />
        public void Increment(string item, long amount)
        {
            _counter.Increment(item, amount);
        }

        /// <inheritdoc />
        public void Decrement()
        {
           _counter.Decrement();
        }

        /// <inheritdoc />
        public void Decrement(string item)
        {
            _counter.Decrement(item);
        }

        /// <inheritdoc />
        public void Decrement(long amount)
        {
           _counter.Decrement(amount);
        }

        /// <inheritdoc />
        public void Decrement(string item, long amount)
        {
            _counter.Decrement(item, amount);
        }
    }
}
