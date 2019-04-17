using App.Metrics;
using App.Metrics.Counter;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using Xunit;

namespace DotNetWorkQueue.AppMetrics.Tests
{
    public class CounterTests
    {
        [Theory, AutoData]
        public void Increment(string name)
        {
            var metrics = Creator.Create();
            var counter = metrics.Provider.Counter.Instance(new CounterOptions() {Name = name});
            var test = Create(counter);
            test.Increment();
            dynamic count = counter;
            Assert.Equal(1, count.Value.Count);
        }

        [Theory, AutoData]
        public void Increment_child(string name, string name2)
        {
            var metrics = Creator.Create();
            var counter = metrics.Provider.Counter.Instance(new CounterOptions() { Name = name });
            var test = Create(counter);
            test.Increment(name2);
            dynamic count = counter;
            Assert.Equal(1, count.Value.Count);
            Assert.Equal(1, count.Value.Items[0].Count);
        }

        [Theory, AutoData]
        public void Increment_Amount(string name, long amount)
        {
            var metrics = Creator.Create();
            var counter = metrics.Provider.Counter.Instance(new CounterOptions() { Name = name });
            var test = Create(counter);
            test.Increment(amount);
            dynamic count = counter;
            Assert.Equal(amount,count.Value.Count);
        }

        [Theory, AutoData]
        public void Increment_Amount_child(string name, string name2, long amount)
        {
            var metrics = Creator.Create();
            var counter = metrics.Provider.Counter.Instance(new CounterOptions() { Name = name });
            var test = Create(counter);
            test.Increment(name2, amount);
            dynamic count = counter;
            Assert.Equal(amount,count.Value.Count);
            Assert.Equal(amount,count.Value.Items[0].Count);
        }

        [Theory, AutoData]
        public void Decrement(string name)
        {
            var metrics = Creator.Create();
            var counter = metrics.Provider.Counter.Instance(new CounterOptions() { Name = name });
            dynamic count = counter;
            var test = Create(counter);
            test.Increment();
            test.Increment();
            test.Increment();
            Assert.Equal(3,count.Value.Count);
            test.Decrement();
            Assert.Equal(2,count.Value.Count);
            test.Decrement();
            Assert.Equal(1, count.Value.Count);
            test.Decrement();
            Assert.Equal(0, count.Value.Count);
        }

        [Theory, AutoData]
        public void Decrement_child(string name, string name2)
        {
            var metrics = Creator.Create();
            var counter = metrics.Provider.Counter.Instance(new CounterOptions() { Name = name });
            dynamic count = counter;
            var test = Create(counter);
            test.Increment(name2);
            test.Increment(name2);
            test.Increment(name2);
            Assert.Equal(3,count.Value.Count);
            Assert.Equal(3,count.Value.Items[0].Count);
            test.Decrement(name2);
            Assert.Equal(2,count.Value.Count);
            Assert.Equal(2,count.Value.Items[0].Count);
            test.Decrement(name2);
            Assert.Equal(1,count.Value.Count);
            Assert.Equal(1,count.Value.Items[0].Count);
            test.Decrement(name2);
            Assert.Equal(0,count.Value.Count);
            Assert.Equal(0,count.Value.Items[0].Count);
        }

        [Theory, AutoData]
        public void Decrement_Amount(string name, long amount)
        {
            var metrics = Creator.Create();
            var counter = metrics.Provider.Counter.Instance(new CounterOptions() { Name = name });
            dynamic count = counter;
            var test = Create(counter);
            test.Increment(amount);
            Assert.Equal(amount, count.Value.Count);
            test.Decrement(amount);
            Assert.Equal(0,count.Value.Count);
        }

        [Theory, AutoData]
        public void Decrement_Amount_child(string name, string name2, long amount)
        {
            var metrics = Creator.Create();
            var counter = metrics.Provider.Counter.Instance(new CounterOptions() { Name = name });
            dynamic count = counter;
            var test = Create(counter);
            test.Increment(name2, amount);
            Assert.Equal(amount, count.Value.Count);
            Assert.Equal(amount, count.Value.Items[0].Count);
            test.Decrement(name2, amount);
            Assert.Equal(0, count.Value.Count);
            Assert.Equal(0, count.Value.Items[0].Count);
        }

        private ICounter Create(App.Metrics.Counter.ICounter counter)
        { 
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(counter);
            return fixture.Create<Counter>();
        }
    }
}
