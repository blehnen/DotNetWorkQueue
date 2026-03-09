using System;
using DotNetWorkQueue.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace DotNetWorkQueue.Tests.Configuration
{
    [TestClass]
    public class HeartBeatThreadPoolConfigurationTests
    {
        [TestMethod]
        public void Test_DefaultNotReadOnly()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<HeartBeatThreadPoolConfiguration>();
            Assert.IsFalse(configuration.IsReadOnly);
        }
        [TestMethod]
        public void Set_Readonly()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<HeartBeatThreadPoolConfiguration>();
            configuration.SetReadOnly();
            Assert.IsTrue(configuration.IsReadOnly);
        }
        [TestMethod]
        public void SetAndGet_HeartBeatThreadsMax()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<HeartBeatThreadPoolConfiguration>();
            var value = fixture.Create<int>();
            configuration.ThreadsMax = value;
            Assert.AreEqual(value, configuration.ThreadsMax);
        }
        [TestMethod]
        public void Set_HeartBeatThreadsMax_WhenReadOnly_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<HeartBeatThreadPoolConfiguration>();
            var value = fixture.Create<int>();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<InvalidOperationException>(
              delegate
              {
                  configuration.ThreadsMax = value;
              });
        }
        [TestMethod]
        public void Set_HeartBeatQueueMax_WhenReadOnly_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<HeartBeatThreadPoolConfiguration>();
            var value = fixture.Create<int>();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<InvalidOperationException>(
              delegate
              {
                  configuration.ThreadsMax = value;
              });
        }
        [TestMethod]
        public void Set_WaitForThreadPoolToFinish_WhenReadOnly_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<HeartBeatThreadPoolConfiguration>();
            var value = fixture.Create<int>();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<InvalidOperationException>(
              delegate
              {
                  configuration.WaitForThreadPoolToFinish = TimeSpan.FromSeconds(value);
              });
        }
    }
}
