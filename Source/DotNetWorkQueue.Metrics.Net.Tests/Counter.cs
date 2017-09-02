// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------

using Metrics;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
using Xunit;

namespace DotNetWorkQueue.Metrics.Net.Tests
{
    public class CounterTests
    {
        [Theory, AutoData]
        public void Increment(string name)
        {
            var counter = Metric.Counter(name, Unit.Bytes);
            var test = Create(counter);
            test.Increment();
            dynamic count = counter;
            Assert.Equal(1, count.Value.Count);
        }

        [Theory, AutoData]
        public void Increment_child(string name, string name2)
        {
            var counter = Metric.Counter(name, name);
            var test = Create(counter);
            test.Increment(name2);
            dynamic count = counter;
            Assert.Equal(1, count.Value.Count);
            Assert.Equal(1, count.Value.Items[0].Count);
        }

        [Theory, AutoData]
        public void Increment_Amount(string name, long amount)
        {
            var counter = Metric.Counter(name, Unit.Bytes);
            var test = Create(counter);
            test.Increment(amount);
            dynamic count = counter;
            Assert.Equal(amount,count.Value.Count);
        }

        [Theory, AutoData]
        public void Increment_Amount_child(string name, string name2, long amount)
        {
            var counter = Metric.Counter(name, name);
            var test = Create(counter);
            test.Increment(name2, amount);
            dynamic count = counter;
            Assert.Equal(amount,count.Value.Count);
            Assert.Equal(amount,count.Value.Items[0].Count);
        }

        [Theory, AutoData]
        public void Decrement(string name)
        {
            var counter = Metric.Counter(name, Unit.Bytes);
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
            var counter = Metric.Counter(name, name);
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
            var counter = Metric.Counter(name, Unit.Bytes);
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
            var counter = Metric.Counter(name, name);
            dynamic count = counter;
            var test = Create(counter);
            test.Increment(name2, amount);
            Assert.Equal(amount, count.Value.Count);
            Assert.Equal(amount, count.Value.Items[0].Count);
            test.Decrement(name2, amount);
            Assert.Equal(0, count.Value.Count);
            Assert.Equal(0, count.Value.Items[0].Count);
        }

        private ICounter Create(global::Metrics.Counter counter)
        { 
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(counter);
            return fixture.Create<Counter>();
        }
    }
}
