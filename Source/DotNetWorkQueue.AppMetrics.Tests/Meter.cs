using App.Metrics.Meter;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.AppMetrics.Tests
{
    [TestClass]
    public class Meter
    {
        [TestMethod]
        public void Mark()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var metrics = Creator.Create();
            var metric = metrics.Provider.Meter.Instance(new MeterOptions() { Name = name });
            var test = Create(metric);
            dynamic dyn = metric;
            test.Mark();
            Assert.AreEqual(1, dyn.Value.Count);
        }

        [TestMethod]
        public void Mark_Child()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var name2 = fixture.Create<string>();
            var metrics = Creator.Create();
            var metric = metrics.Provider.Meter.Instance(new MeterOptions() { Name = name });
            var test = Create(metric);
            dynamic dyn = metric;
            test.Mark(name2);
            Assert.AreEqual(1, dyn.Value.Count);
            Assert.AreEqual(1, dyn.Value.Items[0].Value.Count);
        }

        [TestMethod]
        public void Mark_Value()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var value = fixture.Create<long>();
            var metrics = Creator.Create();
            var metric = metrics.Provider.Meter.Instance(new MeterOptions() { Name = name });
            var test = Create(metric);
            dynamic dyn = metric;
            test.Mark(value);
            Assert.AreEqual(value, dyn.Value.Count);
        }

        [TestMethod]
        public void Mark_Value_Child()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var name2 = fixture.Create<string>();
            var value = fixture.Create<long>();
            var metrics = Creator.Create();
            var metric = metrics.Provider.Meter.Instance(new MeterOptions() { Name = name });
            var test = Create(metric);
            dynamic dyn = metric;
            test.Mark(name2, value);
            Assert.AreEqual(value, dyn.Value.Count);
            Assert.AreEqual(value, dyn.Value.Items[0].Value.Count);
        }

        private IMeter Create(App.Metrics.Meter.IMeter meter)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(meter);
            return fixture.Create<DotNetWorkQueue.AppMetrics.Meter>();
        }
    }
}
