using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.TaskScheduling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.TaskScheduling
{
    [TestClass]
    public class ThreadPoolConfigurationTests
    {
        [TestMethod]
        public void GetSet_IdleTimeout()
        {
            var test = Create();
            test.IdleTimeout = TimeSpan.MaxValue;
            Assert.AreEqual(TimeSpan.MaxValue, test.IdleTimeout);
        }
        [TestMethod]
        public void GetSet_MaxWorkerThreads()
        {
            var test = Create();
            test.MaxWorkerThreads = 5;
            Assert.AreEqual(5, test.MaxWorkerThreads);
        }
        [TestMethod]
        public void GetSet_MinWorkerThreads()
        {
            var test = Create();
            test.MinWorkerThreads = 5;
            Assert.AreEqual(5, test.MinWorkerThreads);
        }
        [TestMethod]
        public void GetSet_WaitForThreadPoolToFinish()
        {
            var test = Create();
            test.WaitForThreadPoolToFinish = TimeSpan.MaxValue;
            Assert.AreEqual(TimeSpan.MaxValue, test.WaitForThreadPoolToFinish);
        }

        private ThreadPoolConfiguration Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<ThreadPoolConfiguration>();
        }
    }
}
