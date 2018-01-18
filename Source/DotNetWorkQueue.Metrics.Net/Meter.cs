namespace DotNetWorkQueue.Metrics.Net
{
    /// <inheritdoc />
    internal class Meter : IMeter
    {
        private readonly global::Metrics.Meter _meter;
        /// <summary>
        /// Initializes a new instance of the <see cref="Meter"/> class.
        /// </summary>
        /// <param name="meter">The meter.</param>
        public Meter(global::Metrics.Meter meter)
        {
            _meter = meter;
        }

        /// <inheritdoc />
        public void Mark()
        {
            _meter.Mark();
        }

        /// <inheritdoc />
        public void Mark(string item)
        {
            _meter.Mark(item);
        }

        /// <inheritdoc />
        public void Mark(long count)
        {
            _meter.Mark(count);
        }

        /// <inheritdoc />
        public void Mark(string item, long count)
        {
            _meter.Mark(item, count);
        }
    }
}
