using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;



using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Configuration
{
    [TestClass]
    public class WorkerConfigurationTests
    {
        [TestMethod]
        public void SetAndGet_AbortWorkerThreadsWhenStopping()
        {
            var configuration = GetConfiguration();
            configuration.AbortWorkerThreadsWhenStopping = true;

            Assert.IsTrue(configuration.AbortWorkerThreadsWhenStopping);
        }
        [TestMethod]
        public void SetAndGet_SingleWorkerWhenNoWorkFound()
        {
            var configuration = GetConfiguration();
            configuration.SingleWorkerWhenNoWorkFound = true;

            Assert.IsTrue(configuration.SingleWorkerWhenNoWorkFound);
        }
        [TestMethod]
        public void SetAndGet_TimeToWaitForWorkersToCancel()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<TimeSpan>();
            var configuration = GetConfiguration();
            configuration.TimeToWaitForWorkersToCancel = value;

            Assert.AreEqual(value, configuration.TimeToWaitForWorkersToCancel);
        }
        [TestMethod]
        public void SetAndGet_TimeToWaitForWorkersToStop()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<TimeSpan>();
            var configuration = GetConfiguration();
            configuration.TimeToWaitForWorkersToStop = value;

            Assert.AreEqual(value, configuration.TimeToWaitForWorkersToStop);
        }
        [TestMethod]
        public void SetAndGet_WorkerCount()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<int>();
            var configuration = GetConfiguration();
            configuration.WorkerCount = value;

            Assert.AreEqual(value, configuration.WorkerCount);
        }
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
        public void Set_AbortWorkerThreadsWhenStopping_WhenReadOnly_Fails()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<InvalidOperationException>(
              delegate
              {
                  configuration.AbortWorkerThreadsWhenStopping = true;
              });
        }
        [TestMethod]
        public void Set_SingleWorkerWhenNoWorkFound_WhenReadOnly_Fails()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<InvalidOperationException>(
              delegate
              {
                  configuration.SingleWorkerWhenNoWorkFound = true;
              });
        }
        [TestMethod]
        public void Set_TimeToWaitForWorkersToCancel_WhenReadOnly_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<TimeSpan>();
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<InvalidOperationException>(
              delegate
              {
                  configuration.TimeToWaitForWorkersToCancel = value;
              });
        }
        [TestMethod]
        public void Set_TimeToWaitForWorkersToStop_WhenReadOnly_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<TimeSpan>();
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<InvalidOperationException>(
              delegate
              {
                  configuration.TimeToWaitForWorkersToStop = value;
              });
        }
        [TestMethod]
        public void Set_WorkerCount_WhenReadOnly_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<int>();
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<InvalidOperationException>(
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
