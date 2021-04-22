using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Xunit;
using AutoFixture.Xunit2;

namespace DotNetWorkQueue.AppMetrics.Tests
{
    public class Metrics
    {
        [Theory, AutoData]
        public void Create(string name, string name2, string name3)
        {
            using (var metric = new DotNetWorkQueue.AppMetrics.Metrics(name))
            {
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

        [Theory, AutoData]
        public void Create2(string name, string name2, string name3)
        {
            using (var metric = new DotNetWorkQueue.AppMetrics.Metrics(CreateAppMetrics()))
            {
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
                Assert.Null(metric.CollectedMetrics);
            }
        }

        private App.Metrics.IMetrics CreateAppMetrics()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<App.Metrics.IMetrics>();
        }
    }
}
