using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using Metrics;
using Xunit;

namespace DotNetWorkQueue.Metrics.Net.Tests
{
    public class Histogram
    {
        [Theory, AutoData]
        public void Update(string name, long value, long value2)
        {
            var metric = Metric.Histogram(name, Unit.Bytes);
            var test = Create(metric);
            dynamic dyn = metric;
            test.Update(value);
            Assert.Equal(1, dyn.Value.Count);
            Assert.Equal(value, dyn.Value.LastValue);
            test.Update(value2);
            Assert.Equal(2, dyn.Value.Count);
            Assert.Equal(value2, dyn.Value.LastValue);
        }

        private IHistogram Create(global::Metrics.Histogram histogram)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(histogram);
            return fixture.Create<Net.Histogram>();
        }
    }
}
