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
using System;
using DotNetWorkQueue.Configuration;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
using Xunit;
namespace DotNetWorkQueue.Tests.Configuration
{
    public class TaskSchedulerConfigurationTests
    {
        [Fact]
        public void Test_DefaultNotReadOnly()
        {
            var configuration = GetConfiguration();
            Assert.Equal(false, configuration.IsReadOnly);
        }
        [Fact]
        public void Set_Readonly()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.Equal(true, configuration.IsReadOnly);
        }
        [Theory, AutoData]
        public void SetAndGet_MaxQueueSize(int value)
        {
            var configuration = GetConfiguration();
            configuration.MaxQueueSize = value;
            Assert.Equal(value, configuration.MaxQueueSize);
        }
        [Theory, AutoData]
        public void SetAndGet_MaximumThreads(int value)
        {
            var configuration = GetConfiguration();
            configuration.MaximumThreads = value;

            Assert.Equal(value, configuration.MaximumThreads);
        }
        [Theory, AutoData]
        public void SetAndGet_MinimumThreads(int value)
        {
            var configuration = GetConfiguration();
            configuration.MinimumThreads = value;

            Assert.Equal(value, configuration.MinimumThreads);
        }
        [Theory, AutoData]
        public void SetAndGet_ThreadIdleTimeout(TimeSpan value)
        {
            var configuration = GetConfiguration();
            configuration.ThreadIdleTimeout = value;

            Assert.Equal(value, configuration.ThreadIdleTimeout);
        }

        [Theory, AutoData]
        public void SetAndGet_WaitForThreadPoolToFinish(TimeSpan value)
        {
            var configuration = GetConfiguration();
            configuration.WaitForTheadPoolToFinish = value;

            Assert.Equal(value, configuration.WaitForTheadPoolToFinish);
        }

        [Theory, AutoData]
        public void Set_MaximumThreads_WhenReadOnly_Fails(int value)
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.MaximumThreads = value;
              });
        }
        [Theory, AutoData]
        public void Set_MinimumThreads_WhenReadOnly_Fails(int value)
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.MinimumThreads = value;
              });
        }
        [Theory, AutoData]
        public void Set_MaxQueueSize_WhenReadOnly_Fails(int value)
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.MaxQueueSize = value;
              });
        }
        [Theory, AutoData]
        public void Set_ThreadIdleTimeout_WhenReadOnly_Fails(TimeSpan value)
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.ThreadIdleTimeout = value;
              });
        }
        [Theory, AutoData]
        public void Set_WaitForThreadPoolToFinish_WhenReadOnly_Fails(TimeSpan value)
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.WaitForTheadPoolToFinish = value;
              });
        }
        private TaskSchedulerConfiguration GetConfiguration()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<TaskSchedulerConfiguration>();
        }
    }
}
