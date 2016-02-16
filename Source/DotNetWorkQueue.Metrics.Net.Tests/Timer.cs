// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System;
using Metrics;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
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
            Action action = () => System.Threading.Thread.Sleep(30);
            test.Time(action);
            Assert.Equal(1, dyn.Value.Rate.Count);
        }

        [Theory, AutoData]
        public void Time_Result(string name)
        {
            var metric = Metric.Timer(name, Unit.Bytes);
            var test = Create(metric);
            dynamic dyn = metric;
            var result = test.Time(() => 1);
            Assert.NotNull(result);
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
                System.Threading.Thread.Sleep(100);
            }
            Assert.Equal(1, dyn.Value.Rate.Count);
        }

        [Theory, AutoData]
        public void NewContext_Action(string name)
        {
            var metric = Metric.Timer(name, Unit.Bytes);
            var test = Create(metric);
            dynamic dyn = metric;
            using (var context = test.NewContext(span => System.Threading.Thread.Sleep(100)))
            {
                System.Threading.Thread.Sleep(100);
                Assert.True(context.Elapsed.TotalMilliseconds > 99, "Elapsed time was less than 99 ms");
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
