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
using DotNetWorkQueue.Configuration;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
using Xunit;
namespace DotNetWorkQueue.Tests.Configuration
{
    public class WorkerConfigurationTests
    {
        [Fact]
        public void SetAndGet_AbortWorkerThreadsWhenStopping()
        {
            var configuration = GetConfiguration();
            configuration.AbortWorkerThreadsWhenStopping = true;

            Assert.Equal(true, configuration.AbortWorkerThreadsWhenStopping);
        }
        [Fact]
        public void SetAndGet_SingleWorkerWhenNoWorkFound()
        {
            var configuration = GetConfiguration();
            configuration.SingleWorkerWhenNoWorkFound = true;

            Assert.Equal(true, configuration.SingleWorkerWhenNoWorkFound);
        }
        [Theory, AutoData]
        public void SetAndGet_TimeToWaitForWorkersToCancel(TimeSpan value)
        {
            var configuration = GetConfiguration();
            configuration.TimeToWaitForWorkersToCancel = value;

            Assert.Equal(value, configuration.TimeToWaitForWorkersToCancel);
        }
        [Theory, AutoData]
        public void SetAndGet_TimeToWaitForWorkersToStop(TimeSpan value)
        {
            var configuration = GetConfiguration();
            configuration.TimeToWaitForWorkersToStop = value;

            Assert.Equal(value, configuration.TimeToWaitForWorkersToStop);
        }
        [Theory, AutoData]
        public void SetAndGet_WorkerCount(int value)
        {
            var configuration = GetConfiguration();
            configuration.WorkerCount = value;

            Assert.Equal(value, configuration.WorkerCount);
        }
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
        [Fact]
        public void Set_AbortWorkerThreadsWhenStopping_WhenReadOnly_Fails()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.AbortWorkerThreadsWhenStopping = true;
              });
        }
        [Fact]
        public void Set_SingleWorkerWhenNoWorkFound_WhenReadOnly_Fails()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.SingleWorkerWhenNoWorkFound = true;
              });
        }
        [Theory, AutoData]
        public void Set_TimeToWaitForWorkersToCancel_WhenReadOnly_Fails(TimeSpan value)
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.TimeToWaitForWorkersToCancel = value;
              });
        }
        [Theory, AutoData]
        public void Set_TimeToWaitForWorkersToStop_WhenReadOnly_Fails(TimeSpan value)
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.TimeToWaitForWorkersToStop = value;
              });
        }
        [Theory, AutoData]
        public void Set_WorkerCount_WhenReadOnly_Fails(int value)
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.WorkerCount = value;
              });
        }
        private WorkerConfiguration GetConfiguration()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<WorkerConfiguration>();
        }
    }
}
