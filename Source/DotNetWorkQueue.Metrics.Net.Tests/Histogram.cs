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
using Metrics;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
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
