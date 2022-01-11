using System;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Configuration;
using Xunit;

namespace DotNetWorkQueue.Tests.Configuration
{
    public class HeartBeatThreadPoolConfigurationTests
    {
        [Theory, AutoData]
        public void Test_DefaultNotReadOnly(HeartBeatThreadPoolConfiguration configuration)
        {
            Assert.False(configuration.IsReadOnly);
        }
        [Theory, AutoData]
        public void Set_Readonly(HeartBeatThreadPoolConfiguration configuration)
        {
            configuration.SetReadOnly();
            Assert.True(configuration.IsReadOnly);
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
                  configuration.ThreadsMax = value;
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
