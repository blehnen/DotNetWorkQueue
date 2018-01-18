using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Configuration;



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

            Assert.True(configuration.AbortWorkerThreadsWhenStopping);
        }
        [Fact]
        public void SetAndGet_SingleWorkerWhenNoWorkFound()
        {
            var configuration = GetConfiguration();
            configuration.SingleWorkerWhenNoWorkFound = true;

            Assert.True(configuration.SingleWorkerWhenNoWorkFound);
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
            Assert.False(configuration.IsReadOnly);
        }
        [Fact]
        public void Set_Readonly()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.True(configuration.IsReadOnly);
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
