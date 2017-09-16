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

using System.Threading;
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
