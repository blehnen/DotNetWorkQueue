using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Configuration;



using Xunit;

namespace DotNetWorkQueue.Tests.Configuration
{
    public class TaskSchedulerConfigurationTests
    {
        [Fact]
        public void Test_DefaultNotReadOnly()
        {
            var configuration = GetConfiguration();
            Assert.False(configuration.IsReadOnly);
        }
        [Fact]
        public void Set_Readonly()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.True(configuration.IsReadOnly);
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
        public void SetAndGet_WaitForThreadPoolToFinish(TimeSpan value)
        {
            var configuration = GetConfiguration();
            configuration.WaitForThreadPoolToFinish = value;

            Assert.Equal(value, configuration.WaitForThreadPoolToFinish);
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
        public void Set_WaitForThreadPoolToFinish_WhenReadOnly_Fails(TimeSpan value)
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.WaitForThreadPoolToFinish = value;
              });
        }
        private TaskSchedulerConfiguration GetConfiguration()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<TaskSchedulerConfiguration>();
        }
    }
}
