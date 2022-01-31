using App.Metrics.Histogram;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using Xunit;

namespace DotNetWorkQueue.AppMetrics.Tests
{
    public class Histogram
    {
        [Theory, AutoData]
        public void Update(string name, long value, long value2)
        {
            var metrics = Creator.Create();
            var metric = metrics.Provider.Histogram.Instance(new HistogramOptions() { Name = name });
            var test = Create(metric);
            dynamic dyn = metric;
            test.Update(value);
            Assert.Equal(1, dyn.Value.Count);
            Assert.Equal(value, dyn.Value.LastValue);
            test.Update(value2);
            Assert.Equal(2, dyn.Value.Count);
            Assert.Equal(value2, dyn.Value.LastValue);
        }

        private IHistogram Create(App.Metrics.Histogram.IHistogram histogram)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(histogram);
            return fixture.Create<DotNetWorkQueue.AppMetrics.Histogram>();
        }
    }
}
