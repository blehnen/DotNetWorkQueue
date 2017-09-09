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
using Ploeh.AutoFixture.Xunit2;
using Xunit;

namespace DotNetWorkQueue.Tests.Configuration
{
    public class HeartBeatThreadPoolConfigurationTests
    {
        [Theory, AutoData]
        public void Test_DefaultNotReadOnly(HeartBeatThreadPoolConfiguration configuration)
        {
            Assert.Equal(false, configuration.IsReadOnly);
        }
        [Theory, AutoData]
        public void Set_Readonly(HeartBeatThreadPoolConfiguration configuration)
        {
            configuration.SetReadOnly();
            Assert.Equal(true, configuration.IsReadOnly);
        }
        [Theory, AutoData]
        public void SetAndGet_QueueMax(HeartBeatThreadPoolConfiguration configuration, int value)
        {
            configuration.QueueMax = value;
            Assert.Equal(value, configuration.QueueMax);
        }
        [Theory, AutoData]
        public void SetAndGet_HeartBeatThreadsMax(HeartBeatThreadPoolConfiguration configuration, int value)
        {
            configuration.ThreadsMax = value;
            Assert.Equal(value, configuration.ThreadsMax);
        }
        [Theory, AutoData]
        public void Set_HeartBeatThreadsMax_WhenReadOnly_Fails(HeartBeatThreadPoolConfiguration configuration, int value)
        {
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.ThreadsMax = value;
              });
        }
        [Theory, AutoData]
        public void Set_HeartBeatQueueMax_WhenReadOnly_Fails(HeartBeatThreadPoolConfiguration configuration, int value)
        {
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.QueueMax = value;
              });
        }
        [Theory, AutoData]
        public void Set_WaitForThreadPoolToFinish_WhenReadOnly_Fails(HeartBeatThreadPoolConfiguration configuration, int value)
        {
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.WaitForThreadPoolToFinish = TimeSpan.FromSeconds(value);
              });
        }
    }
}
