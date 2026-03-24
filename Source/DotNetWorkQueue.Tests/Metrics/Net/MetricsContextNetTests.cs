using System;
using System.Diagnostics.Metrics;
using DotNetWorkQueue.Metrics.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Metrics.Net
{
    [TestClass]
    public class MetricsContextNetTests
    {
        [TestMethod]
        public void Context_Returns_Child_Context()
        {
            var test = Create();
            var result = test.Context("child");
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Context_Returns_Same_Instance_For_Same_Name()
        {
            var test = Create();
            var first = test.Context("child");
            var second = test.Context("child");
            Assert.AreSame(first, second);
        }

        [TestMethod]
        public void Context_Returns_Different_Instance_For_Different_Name()
        {
            var test = Create();
            var first = test.Context("child1");
            var second = test.Context("child2");
            Assert.AreNotSame(first, second);
        }

        [TestMethod]
        public void ShutdownContext_Removes_Child()
        {
            var test = Create();
            var first = test.Context("child");
            test.ShutdownContext("child");
            var second = test.Context("child");
            Assert.AreNotSame(first, second);
        }

        [TestMethod]
        public void Counter_Returns_Instance()
        {
            var test = Create();
            var result = test.Counter("test", Units.Items);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Counter_Returns_Same_Instance_For_Same_Name()
        {
            var test = Create();
            var first = test.Counter("test", Units.Items);
            var second = test.Counter("test", Units.Items);
            Assert.AreSame(first, second);
        }

        [TestMethod]
        public void Counter_With_String_Unit_Returns_Instance()
        {
            var test = Create();
            var result = test.Counter("test", "Items");
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Meter_Returns_Instance()
        {
            var test = Create();
            var result = test.Meter("test", Units.Items, TimeUnits.Seconds);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Meter_Returns_Same_Instance_For_Same_Name()
        {
            var test = Create();
            var first = test.Meter("test", Units.Items, TimeUnits.Seconds);
            var second = test.Meter("test", Units.Items, TimeUnits.Seconds);
            Assert.AreSame(first, second);
        }

        [TestMethod]
        public void Meter_With_String_Unit_Returns_Instance()
        {
            var test = Create();
            var result = test.Meter("test", "Items", TimeUnits.Seconds);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Histogram_Returns_Instance()
        {
            var test = Create();
            var result = test.Histogram("test", Units.Items);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Timer_Returns_Instance()
        {
            var test = Create();
            var result = test.Timer("test", Units.Items, TimeUnits.Seconds, TimeUnits.Milliseconds);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Gauge_Does_Not_Throw()
        {
            var test = Create();
            test.Gauge("test", () => 1.0, Units.None);
        }

        [TestMethod]
        public void Dispose_Does_Not_Throw()
        {
            var test = Create();
            test.Dispose();
        }

        private MetricsContextNet Create()
        {
            return new MetricsContextNet(new Meter("TestMeter." + Guid.NewGuid()));
        }
    }
}
