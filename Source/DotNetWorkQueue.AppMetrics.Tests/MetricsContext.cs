using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace DotNetWorkQueue.AppMetrics.Tests
{
    [TestClass]
    public class MetricsContext
    {
        [TestMethod]
        public void Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var name2 = fixture.Create<string>();
            var name3 = fixture.Create<string>();
            var contextName = fixture.Create<string>();
            using (var parent = new DotNetWorkQueue.AppMetrics.Metrics(name))
            {
                using (var metric = parent.Context(contextName))
                {
                    metric.Gauge(name, () => 1, Units.Bytes);
                    Assert.IsNotNull(metric.Meter(name, Units.Bytes));
                    Assert.IsNotNull(metric.Meter(name2, name2, TimeUnits.Seconds));
                    Assert.IsNotNull(metric.Counter(name, Units.Bytes));
                    Assert.IsNotNull(metric.Counter(name2, name2));
                    Assert.IsNotNull(metric.Histogram(name, Units.Bytes));
                    Assert.IsNotNull(metric.Timer(name, Units.Bytes, SamplingTypes.FavorRecent, TimeUnits.Seconds,
                        TimeUnits.Seconds));

                    var context = metric.Context(name3);
                    Assert.IsNotNull(context);
                    metric.ShutdownContext(name3);
                }
            }
        }
    }
}
