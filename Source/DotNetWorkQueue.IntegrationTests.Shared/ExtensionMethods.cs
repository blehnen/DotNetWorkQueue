namespace DotNetWorkQueue.IntegrationTests.Shared
{
    /// <summary>
    /// Extension methods for obtaining the current metrics
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Gets the current metric values
        /// </summary>
        /// <param name="data">The data.</param>
        public static MetricsSnapshot GetCurrentMetrics(this IMetrics data)
        {
            return data.GetCollectedMetrics();
        }
    }
}
