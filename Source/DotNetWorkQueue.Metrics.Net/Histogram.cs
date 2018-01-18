namespace DotNetWorkQueue.Metrics.Net
{
    /// <inheritdoc />
    internal class Histogram : IHistogram
    {
        private readonly global::Metrics.Histogram _histogram;
        /// <summary>
        /// Initializes a new instance of the <see cref="Histogram"/> class.
        /// </summary>
        /// <param name="histogram">The histogram.</param>
        public Histogram(global::Metrics.Histogram histogram)
        {
            _histogram = histogram;
        }
        /// <inheritdoc />
        public void Update(long value, string userValue = null)
        {
            _histogram.Update(value, userValue);
        }
    }
}
