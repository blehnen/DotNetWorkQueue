using DotNetWorkQueue.Metrics.NoOp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Metrics.NoOp
{
    [TestClass]
    public class MetricsNoOpTests
    {
        [TestMethod]
        public void Test_Context()
        {
            var test = Create();
            var result = test.Context("test");
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Test_ShutdownContext()
        {
            var test = Create();
            test.ShutdownContext("test");
        }

        [TestMethod]
        public void Test_Gauge()
        {
            var test = Create();
            test.Gauge("test", () => 0.0d, Units.Bytes, null);
        }

        [TestMethod]
        public void Test_Meter()
        {
            var test = Create();
            var result = test.Meter("test", Units.Bytes, TimeUnits.Days);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Test_Meter2()
        {
            var test = Create();
            var result = test.Meter("test", "Bytes", TimeUnits.Days);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Test_Counter()
        {
            var test = Create();
            var result = test.Counter("test", Units.Bytes);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Test_Counter2()
        {
            var test = Create();
            var result = test.Counter("test", "Bytes");
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Test_Histogram()
        {
            var test = Create();
            var result = test.Histogram("testing", Units.Bytes);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Test_Timer()
        {
            var test = Create();
            var result = test.Timer("Testing", Units.Bytes, TimeUnits.Minutes, TimeUnits.Milliseconds);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Test_Dispose()
        {
            var test = Create();
            test.Dispose();
        }

        [TestMethod]
        public void Test_CollectedMetrics()
        {
            var test = Create();
            var result = test.GetCollectedMetrics();
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Counters.Count);
            Assert.AreEqual(0, result.Meters.Count);
        }

        private MetricsNoOp Create()
        {
            return new MetricsNoOp();
        }
    }
}
