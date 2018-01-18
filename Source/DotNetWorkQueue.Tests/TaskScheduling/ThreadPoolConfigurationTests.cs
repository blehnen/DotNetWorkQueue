using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.TaskScheduling;
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
        public void GetSet_WaitForThreadPoolToFinish()
        {
            var test = Create();
            test.WaitForThreadPoolToFinish = TimeSpan.MaxValue;
            Assert.Equal(TimeSpan.MaxValue, test.WaitForThreadPoolToFinish);
        }

        private ThreadPoolConfiguration Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<ThreadPoolConfiguration>();
        }
    }
}
