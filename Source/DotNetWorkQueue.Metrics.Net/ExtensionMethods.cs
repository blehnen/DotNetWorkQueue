using System.Collections.Generic;
using System.Linq;
using Metrics;
using Metrics.MetricData;

namespace DotNetWorkQueue.Metrics.Net
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
        public static MetricsData GetCurrentMetrics(this IMetrics data)
        {
            return (MetricsData)data.CollectedMetrics;
        }
        /// <summary>
        /// Gets the current metrics.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static MetricsData GetCurrentMetrics(this Metrics data)
        {
            return (MetricsData)data.CollectedMetrics;
        }

        /// <summary>
        /// Metrics.net only supports a single string for a tag. We will pull the first value out of the list.
        /// </summary>
        /// <param name="tags">The tags.</param>
        /// <returns></returns>
        public static string GetFirstTag(this List<KeyValuePair<string, string>> tags)
        {
            if (tags == null || tags.Count == 0) return null;
            return tags[0].Value;
        }
    }
}
