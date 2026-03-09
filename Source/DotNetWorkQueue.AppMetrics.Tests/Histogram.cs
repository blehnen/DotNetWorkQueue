using App.Metrics.Histogram;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.AppMetrics.Tests
{
    [TestClass]
    public class Histogram
    {
        [TestMethod]
        public void Update()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var value = fixture.Create<long>();
            var value2 = fixture.Create<long>();
            var metrics = Creator.Create();
            var metric = metrics.Provider.Histogram.Instance(new HistogramOptions() { Name = name });
            var test = Create(metric);
            dynamic dyn = metric;
            test.Update(value);
            Assert.AreEqual(1, dyn.Value.Count);
            Assert.AreEqual(value, dyn.Value.LastValue);
            test.Update(value2);
            Assert.AreEqual(2, dyn.Value.Count);
            Assert.AreEqual(value2, dyn.Value.LastValue);
        }

        private IHistogram Create(App.Metrics.Histogram.IHistogram histogram)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(histogram);
            return fixture.Create<DotNetWorkQueue.AppMetrics.Histogram>();
        }
    }
}
