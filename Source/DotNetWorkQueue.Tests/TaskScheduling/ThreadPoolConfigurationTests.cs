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
using DotNetWorkQueue.TaskScheduling;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
namespace DotNetWorkQueue.Tests.TaskScheduling
{
    public class ThreadPoolConfigurationTests
    {
        [Fact]
        public void GetSet_IdleTimeout()
        {
            var test = Create();
            test.IdleTimeout = TimeSpan.MaxValue;
            Assert.Equal(TimeSpan.MaxValue, test.IdleTimeout);
        }
        [Fact]
        public void GetSet_MaxWorkerThreads()
        {
            var test = Create();
            test.MaxWorkerThreads = 5;
            Assert.Equal(5, test.MaxWorkerThreads);
        }
        [Fact]
        public void GetSet_MinWorkerThreads()
        {
            var test = Create();
            test.MinWorkerThreads = 5;
            Assert.Equal(5, test.MinWorkerThreads);
        }
        [Fact]
        public void GetSet_WaitForTheadPoolToFinish()
        {
            var test = Create();
            test.WaitForTheadPoolToFinish = TimeSpan.MaxValue;
            Assert.Equal(TimeSpan.MaxValue, test.WaitForTheadPoolToFinish);
        }

        private ThreadPoolConfiguration Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<ThreadPoolConfiguration>();
        }
    }
}
