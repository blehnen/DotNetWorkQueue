using AutoFixture.Xunit2;
using Xunit;

namespace DotNetWorkQueue.Metrics.Net.Tests
{
    public class Metrics
    {
        [Theory, AutoData]
        public void Create(string name, string name2, string name3)
        {
            using (var metric = new Net.Metrics(name))
            {
                Assert.NotNull(metric.Config);
                metric.Gauge(name, () => 1, Units.Bytes);
                Assert.NotNull(metric.Meter(name, Units.Bytes, TimeUnits.Seconds));
                Assert.NotNull(metric.Meter(name2, name2, TimeUnits.Seconds));
                Assert.NotNull(metric.Counter(name, Units.Bytes));
                Assert.NotNull(metric.Counter(name2, name2));
                Assert.NotNull(metric.Histogram(name, Units.Bytes, SamplingTypes.FavorRecent));
                Assert.NotNull(metric.Timer(name, Units.Bytes, SamplingTypes.FavorRecent, TimeUnits.Seconds, TimeUnits.Seconds));

                var context = metric.Context(name3);
                Assert.NotNull(context);
                metric.ShutdownContext(name3);
                Assert.NotNull(metric.CollectedMetrics);
            }
        }
    }
}
