using System.Collections.Concurrent;

namespace DotNetWorkQueue.IntegrationTests.Metrics
{
    public class MetricsData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsData"/> class.
        /// </summary>
        /// <param name="meter">The meter.</param>
        /// <param name="counter">The counter.</param>
        public MetricsData(ConcurrentDictionary<string, Meter> meter,
            ConcurrentDictionary<string, Counter> counter)
        {
            Meters = meter;
            Counters = counter;
        }
        public ConcurrentDictionary<string, Meter> Meters { get; }
        public ConcurrentDictionary<string, Counter> Counters { get; }
    }
}
