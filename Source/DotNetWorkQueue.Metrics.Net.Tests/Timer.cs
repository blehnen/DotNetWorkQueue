using System.Threading;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using Metrics;
using Xunit;

namespace DotNetWorkQueue.Metrics.Net.Tests
{
    public class Timer
    {
        [Theory, AutoData]
        public void Record(string name, long value)
        {
            var metric = Metric.Timer(name, Unit.Bytes);
            var test = Create(metric);
            dynamic dyn = metric;
            test.Record(value, TimeUnits.Seconds);
            Assert.Equal(1, dyn.Value.Rate.Count);
        }

        [Theory, AutoData]
        public void Time(string name)
        {
            var metric = Metric.Timer(name, Unit.Bytes);
            var test = Create(metric);
            dynamic dyn = metric;
            test.Time(() => 1);
            Assert.Equal(1, dyn.Value.Rate.Count);
        }

        [Theory, AutoData]
        public void Time_Action(string name)
        {
            var metric = Metric.Timer(name, Unit.Bytes);
            var test = Create(metric);
            dynamic dyn = metric;
            void Action() => Thread.Sleep(30);
            test.Time(Action);
            Assert.Equal(1, dyn.Value.Rate.Count);
        }

        [Theory, AutoData]
        public void NewContext(string name)
        {
            var metric = Metric.Timer(name, Unit.Bytes);
            var test = Create(metric);
            dynamic dyn = metric;
            using (test.NewContext())
            {
                Thread.Sleep(100);
            }
            Assert.Equal(1, dyn.Value.Rate.Count);
        }

        private ITimer Create(global::Metrics.Timer timer)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(timer);
            return fixture.Create<Net.Timer>();
        }
    }
}
