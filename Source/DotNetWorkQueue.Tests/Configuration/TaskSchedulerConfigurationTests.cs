using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;



using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Configuration
{
    [TestClass]
    public class TaskSchedulerConfigurationTests
    {
        [TestMethod]
        public void Test_DefaultNotReadOnly()
        {
            var configuration = GetConfiguration();
            Assert.IsFalse(configuration.IsReadOnly);
        }
        [TestMethod]
        public void Set_Readonly()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.IsTrue(configuration.IsReadOnly);
        }
        [TestMethod]
        public void SetAndGet_MaxQueueSize()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<int>();
            var configuration = GetConfiguration();
            configuration.MaxQueueSize = value;
            Assert.AreEqual(value, configuration.MaxQueueSize);
        }
        [TestMethod]
        public void SetAndGet_MaximumThreads()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<int>();
            var configuration = GetConfiguration();
            configuration.MaximumThreads = value;

            Assert.AreEqual(value, configuration.MaximumThreads);
        }
        [TestMethod]
        public void SetAndGet_WaitForThreadPoolToFinish()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<TimeSpan>();
            var configuration = GetConfiguration();
            configuration.WaitForThreadPoolToFinish = value;

            Assert.AreEqual(value, configuration.WaitForThreadPoolToFinish);
        }

        [TestMethod]
        public void Set_MaximumThreads_WhenReadOnly_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<int>();
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<InvalidOperationException>(
              delegate
              {
                  configuration.MaximumThreads = value;
              });
        }
        [TestMethod]
        public void Set_MaxQueueSize_WhenReadOnly_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<int>();
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<InvalidOperationException>(
              delegate
              {
                  configuration.MaxQueueSize = value;
              });
        }
        [TestMethod]
        public void Set_WaitForThreadPoolToFinish_WhenReadOnly_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<TimeSpan>();
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.ThrowsExactly<InvalidOperationException>(
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
