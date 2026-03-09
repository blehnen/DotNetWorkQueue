using App.Metrics.Counter;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.AppMetrics.Tests
{
    [TestClass]
    public class CounterTests
    {
        [TestMethod]
        public void Increment()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var metrics = Creator.Create();
            var counter = metrics.Provider.Counter.Instance(new CounterOptions() { Name = name });
            var test = Create(counter);
            test.Increment();
            dynamic count = counter;
            Assert.AreEqual(1, count.Value.Count);
        }

        [TestMethod]
        public void Increment_child()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var name2 = fixture.Create<string>();
            var metrics = Creator.Create();
            var counter = metrics.Provider.Counter.Instance(new CounterOptions() { Name = name });
            var test = Create(counter);
            test.Increment(name2);
            dynamic count = counter;
            Assert.AreEqual(1, count.Value.Count);
            Assert.AreEqual(1, count.Value.Items[0].Count);
        }

        [TestMethod]
        public void Increment_Amount()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var amount = fixture.Create<long>();
            var metrics = Creator.Create();
            var counter = metrics.Provider.Counter.Instance(new CounterOptions() { Name = name });
            var test = Create(counter);
            test.Increment(amount);
            dynamic count = counter;
            Assert.AreEqual(amount, count.Value.Count);
        }

        [TestMethod]
        public void Increment_Amount_child()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var name2 = fixture.Create<string>();
            var amount = fixture.Create<long>();
            var metrics = Creator.Create();
            var counter = metrics.Provider.Counter.Instance(new CounterOptions() { Name = name });
            var test = Create(counter);
            test.Increment(name2, amount);
            dynamic count = counter;
            Assert.AreEqual(amount, count.Value.Count);
            Assert.AreEqual(amount, count.Value.Items[0].Count);
        }

        [TestMethod]
        public void Decrement()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var metrics = Creator.Create();
            var counter = metrics.Provider.Counter.Instance(new CounterOptions() { Name = name });
            dynamic count = counter;
            var test = Create(counter);
            test.Increment();
            test.Increment();
            test.Increment();
            Assert.AreEqual(3, count.Value.Count);
            test.Decrement();
            Assert.AreEqual(2, count.Value.Count);
            test.Decrement();
            Assert.AreEqual(1, count.Value.Count);
            test.Decrement();
            Assert.AreEqual(0, count.Value.Count);
        }

        [TestMethod]
        public void Decrement_child()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var name2 = fixture.Create<string>();
            var metrics = Creator.Create();
            var counter = metrics.Provider.Counter.Instance(new CounterOptions() { Name = name });
            dynamic count = counter;
            var test = Create(counter);
            test.Increment(name2);
            test.Increment(name2);
            test.Increment(name2);
            Assert.AreEqual(3, count.Value.Count);
            Assert.AreEqual(3, count.Value.Items[0].Count);
            test.Decrement(name2);
            Assert.AreEqual(2, count.Value.Count);
            Assert.AreEqual(2, count.Value.Items[0].Count);
            test.Decrement(name2);
            Assert.AreEqual(1, count.Value.Count);
            Assert.AreEqual(1, count.Value.Items[0].Count);
            test.Decrement(name2);
            Assert.AreEqual(0, count.Value.Count);
            Assert.AreEqual(0, count.Value.Items[0].Count);
        }

        [TestMethod]
        public void Decrement_Amount()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var amount = fixture.Create<long>();
            var metrics = Creator.Create();
            var counter = metrics.Provider.Counter.Instance(new CounterOptions() { Name = name });
            dynamic count = counter;
            var test = Create(counter);
            test.Increment(amount);
            Assert.AreEqual(amount, count.Value.Count);
            test.Decrement(amount);
            Assert.AreEqual(0, count.Value.Count);
        }

        [TestMethod]
        public void Decrement_Amount_child()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var name2 = fixture.Create<string>();
            var amount = fixture.Create<long>();
            var metrics = Creator.Create();
            var counter = metrics.Provider.Counter.Instance(new CounterOptions() { Name = name });
            dynamic count = counter;
            var test = Create(counter);
            test.Increment(name2, amount);
            Assert.AreEqual(amount, count.Value.Count);
            Assert.AreEqual(amount, count.Value.Items[0].Count);
            test.Decrement(name2, amount);
            Assert.AreEqual(0, count.Value.Count);
            Assert.AreEqual(0, count.Value.Items[0].Count);
        }

        private ICounter Create(App.Metrics.Counter.ICounter counter)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(counter);
            return fixture.Create<Counter>();
        }
    }
}
