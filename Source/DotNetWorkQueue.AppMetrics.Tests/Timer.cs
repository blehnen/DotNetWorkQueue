using System.Threading;
using App.Metrics.Timer;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.AppMetrics.Tests
{
    [TestClass]
    public class Timer
    {
        [TestMethod]
        public void Record()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var value = fixture.Create<long>();
            var metrics = Creator.Create();
            var metric = metrics.Provider.Timer.Instance(new TimerOptions() { Name = name });
            var test = Create(metric);
            dynamic dyn = metric;
            test.Record(value, TimeUnits.Seconds);
            Assert.AreEqual(1, dyn.Value.Rate.Count);
        }

        [TestMethod]
        public void Time()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var metrics = Creator.Create();
            var metric = metrics.Provider.Timer.Instance(new TimerOptions() { Name = name });
            var test = Create(metric);
            dynamic dyn = metric;
            test.Time(() => 1);
            Assert.AreEqual(1, dyn.Value.Rate.Count);
        }

        [TestMethod]
        public void Time_Action()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var metrics = Creator.Create();
            var metric = metrics.Provider.Timer.Instance(new TimerOptions() { Name = name });
            var test = Create(metric);
            dynamic dyn = metric;
            void Action() => Thread.Sleep(30);
            test.Time(Action);
            Assert.AreEqual(1, dyn.Value.Rate.Count);
        }

        [TestMethod]
        public void NewContext()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var metrics = Creator.Create();
            var metric = metrics.Provider.Timer.Instance(new TimerOptions() { Name = name });
            var test = Create(metric);
            dynamic dyn = metric;
            using (test.NewContext())
            {
                Thread.Sleep(100);
            }
            Assert.AreEqual(1, dyn.Value.Rate.Count);
        }

        private ITimer Create(App.Metrics.Timer.ITimer timer)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(timer);
            return fixture.Create<AppMetrics.Timer>();
        }
    }
}
