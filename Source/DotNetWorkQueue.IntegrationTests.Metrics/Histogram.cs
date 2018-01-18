namespace DotNetWorkQueue.IntegrationTests.Metrics
{
    /// <inheritdoc />
    internal class Histogram : IHistogram
    {
        /// <inheritdoc />
        public void Update(long value, string userValue = null)
        {
           //noop
        }
    }
}
