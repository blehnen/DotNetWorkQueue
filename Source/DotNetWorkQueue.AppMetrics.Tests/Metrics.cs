using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.AppMetrics.Tests
{
    [TestClass]
    public class Metrics
    {
        [TestMethod]
        public void Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var name2 = fixture.Create<string>();
            var name3 = fixture.Create<string>();
            using (var metric = new DotNetWorkQueue.AppMetrics.Metrics(name))
            {
                metric.Gauge(name, () => 1, Units.Bytes);
                Assert.IsNotNull(metric.Meter(name, Units.Bytes, TimeUnits.Seconds));
                Assert.IsNotNull(metric.Meter(name2, name2, TimeUnits.Seconds));
                Assert.IsNotNull(metric.Counter(name, Units.Bytes));
                Assert.IsNotNull(metric.Counter(name2, name2));
                Assert.IsNotNull(metric.Histogram(name, Units.Bytes, SamplingTypes.FavorRecent));
                Assert.IsNotNull(metric.Timer(name, Units.Bytes, SamplingTypes.FavorRecent, TimeUnits.Seconds, TimeUnits.Seconds));

                var context = metric.Context(name3);
                Assert.IsNotNull(context);
                metric.ShutdownContext(name3);
                Assert.IsNotNull(metric.CollectedMetrics);
            }
        }

        [TestMethod]
        public void Create2()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var name2 = fixture.Create<string>();
            var name3 = fixture.Create<string>();
            using (var metric = new DotNetWorkQueue.AppMetrics.Metrics(CreateAppMetrics()))
            {
                metric.Gauge(name, () => 1, Units.Bytes);
                Assert.IsNotNull(metric.Meter(name, Units.Bytes, TimeUnits.Seconds));
                Assert.IsNotNull(metric.Meter(name2, name2, TimeUnits.Seconds));
                Assert.IsNotNull(metric.Counter(name, Units.Bytes));
                Assert.IsNotNull(metric.Counter(name2, name2));
                Assert.IsNotNull(metric.Histogram(name, Units.Bytes, SamplingTypes.FavorRecent));
                Assert.IsNotNull(metric.Timer(name, Units.Bytes, SamplingTypes.FavorRecent, TimeUnits.Seconds, TimeUnits.Seconds));

                var context = metric.Context(name3);
                Assert.IsNotNull(context);
                metric.ShutdownContext(name3);
                Assert.IsNull(metric.CollectedMetrics);
            }
        }

        private App.Metrics.IMetrics CreateAppMetrics()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<App.Metrics.IMetrics>();
        }
    }
}
