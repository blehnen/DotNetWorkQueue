using AutoFixture.Xunit2;
using Xunit;

namespace DotNetWorkQueue.AppMetrics.Tests
{
    public class MetricsContext
    {
        [Theory, AutoData]
        public void Create(string name, string name2, string name3, string contextName)
        {
            using (var parent = new DotNetWorkQueue.AppMetrics.Metrics(name))
            {
                using (var metric = parent.Context(contextName))
                {
                    metric.Gauge(name, () => 1, Units.Bytes);
                    Assert.NotNull(metric.Meter(name, Units.Bytes));
                    Assert.NotNull(metric.Meter(name2, name2, TimeUnits.Seconds));
                    Assert.NotNull(metric.Counter(name, Units.Bytes));
                    Assert.NotNull(metric.Counter(name2, name2));
                    Assert.NotNull(metric.Histogram(name, Units.Bytes));
                    Assert.NotNull(metric.Timer(name, Units.Bytes, SamplingTypes.FavorRecent, TimeUnits.Seconds,
                        TimeUnits.Seconds));

                    var context = metric.Context(name3);
                    Assert.NotNull(context);
                    metric.ShutdownContext(name3);
                }
            }
        }
    }
}
