using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using DotNetWorkQueue.Metrics.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Metrics.Net
{
    [TestClass]
    public class TimerNetTests
    {
        [TestMethod]
        public void Record_Does_Not_Throw()
        {
            var test = Create();
            test.Record(100, TimeUnits.Milliseconds);
        }

        [TestMethod]
        public void Time_Action_Executes_Action()
        {
            var test = Create();
            var executed = false;
            test.Time(() => { executed = true; });
            Assert.IsTrue(executed);
        }

        [TestMethod]
        public void Time_Func_Returns_Result()
        {
            var test = Create();
            var result = test.Time(() => 42);
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        public void NewContext_Returns_Instance()
        {
            var test = Create();
            var result = test.NewContext();
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Dispose_Does_Not_Throw()
        {
            var test = Create();
            test.Dispose();
        }

        private TimerNet Create()
        {
            var meter = new Meter("TestMeter." + Guid.NewGuid());
            var histogram = meter.CreateHistogram<double>("test_timer", "ms");
            return new TimerNet(histogram, Array.Empty<KeyValuePair<string, object>>());
        }
    }
}
