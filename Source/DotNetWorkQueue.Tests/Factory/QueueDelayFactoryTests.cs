// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
using System.Collections.Generic;
using DotNetWorkQueue.Factory;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class QueueDelayFactoryTests
    {
        [Fact]
        public void Create_Default()
        {
            var factory = Create();
            var test = factory.Create();
            Assert.NotNull(test);
        }
        [Fact]
        public void Create_Default_TimeSpans()
        {
            var factory = Create();
            var list = new List<TimeSpan>
            {
                TimeSpan.FromHours(1),
                TimeSpan.FromHours(2),
                TimeSpan.FromHours(3)
            };
            var test = factory.Create(list);
            var i = 0;
            foreach (var t in test)
            {
                Assert.Equal(t, TimeSpan.FromHours(i + 1));
                i++;
            }
        }
        public IQueueDelayFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<QueueDelayFactory>();
        }
    }
}
