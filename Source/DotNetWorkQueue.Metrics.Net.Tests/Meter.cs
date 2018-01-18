using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using Metrics;
using Xunit;

namespace DotNetWorkQueue.Metrics.Net.Tests
{
    public class Meter
    {
        [Theory, AutoData]
        public void Mark(string name)
        {
            var metric = Metric.Meter(name, Unit.Bytes);
            var test = Create(metric);
            dynamic dyn = metric;
            test.Mark();
            Assert.Equal(1, dyn.Value.Count);
        }

        [Theory, AutoData]
        public void Mark_Child(string name, string name2)
        {
            var metric = Metric.Meter(name, Unit.Bytes);
            var test = Create(metric);
            dynamic dyn = metric;
            test.Mark(name2);
            Assert.Equal(1, dyn.Value.Count);
            Assert.Equal(1, dyn.Value.Items[0].Value.Count);
        }

        [Theory, AutoData]
        public void Mark_Value(string name, long value)
        {
            var metric = Metric.Meter(name, Unit.Bytes);
            var test = Create(metric);
            dynamic dyn = metric;
            test.Mark(value);
            Assert.Equal(value, dyn.Value.Count);
        }

        [Theory, AutoData]
        public void Mark_Value_Child(string name, string name2, long value)
        {
            var metric = Metric.Meter(name, Unit.Bytes);
            var test = Create(metric);
            dynamic dyn = metric;
            test.Mark(name2, value);
            Assert.Equal(value, dyn.Value.Count);
            Assert.Equal(value, dyn.Value.Items[0].Value.Count);
        }

        private IMeter Create(global::Metrics.Meter meter)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(meter);
            return fixture.Create<Net.Meter>();
        }
    }
}
