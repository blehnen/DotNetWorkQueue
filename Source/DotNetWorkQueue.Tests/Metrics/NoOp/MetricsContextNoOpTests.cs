using System;
using System.Collections.Generic;
using DotNetWorkQueue.Metrics.NoOp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Metrics.NoOp
{
    [TestClass]
    public class MetricsContextNoOpTests
    {
        [TestMethod]
        public void Context_Returns_NonNull()
        {
            var test = Create();
            var result = test.Context("test");
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Context_Returns_New_Instance()
        {
            var test = Create();
            var result = test.Context("test");
            Assert.IsInstanceOfType<MetricsContextNoOp>(result);
        }

        [TestMethod]
        public void ShutdownContext_Does_Not_Throw()
        {
            var test = Create();
            test.ShutdownContext("test");
        }

        [TestMethod]
        public void Gauge_Does_Not_Throw()
        {
            var test = Create();
            test.Gauge("test", () => 1.0d, Units.Bytes);
        }

        [TestMethod]
        public void Gauge_With_Tags_Does_Not_Throw()
        {
            var test = Create();
            var tags = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("key", "value")
            };
            test.Gauge("test", () => 1.0d, Units.Bytes, tags);
        }

        [TestMethod]
        public void Counter_With_Units_Returns_NonNull()
        {
            var test = Create();
            var result = test.Counter("test", Units.Bytes);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Counter_With_UnitName_Returns_NonNull()
        {
            var test = Create();
            var result = test.Counter("test", "Bytes");
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Counter_Returns_Same_Instance()
        {
            var test = Create();
            var result1 = test.Counter("test1", Units.Bytes);
            var result2 = test.Counter("test2", Units.Bytes);
            Assert.AreSame(result1, result2);
        }

        [TestMethod]
        public void Meter_With_Units_Returns_NonNull()
        {
            var test = Create();
            var result = test.Meter("test", Units.Bytes, TimeUnits.Seconds);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Meter_With_UnitName_Returns_NonNull()
        {
            var test = Create();
            var result = test.Meter("test", "Bytes", TimeUnits.Seconds);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Meter_Returns_Same_Instance()
        {
            var test = Create();
            var result1 = test.Meter("test1", Units.Bytes, TimeUnits.Seconds);
            var result2 = test.Meter("test2", Units.Bytes, TimeUnits.Seconds);
            Assert.AreSame(result1, result2);
        }

        [TestMethod]
        public void Histogram_Returns_NonNull()
        {
            var test = Create();
            var result = test.Histogram("test", Units.Bytes);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Histogram_Returns_Same_Instance()
        {
            var test = Create();
            var result1 = test.Histogram("test1", Units.Bytes);
            var result2 = test.Histogram("test2", Units.Bytes);
            Assert.AreSame(result1, result2);
        }

        [TestMethod]
        public void Timer_Returns_NonNull()
        {
            var test = Create();
            var result = test.Timer("test", Units.Bytes, TimeUnits.Seconds, TimeUnits.Milliseconds);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Timer_Returns_Same_Instance()
        {
            var test = Create();
            var result1 = test.Timer("test1", Units.Bytes);
            var result2 = test.Timer("test2", Units.Bytes);
            Assert.AreSame(result1, result2);
        }

        [TestMethod]
        public void Dispose_Does_Not_Throw()
        {
            var test = Create();
            test.Dispose();
        }

        [TestMethod]
        public void Dispose_Is_Idempotent()
        {
            var test = Create();
            test.Dispose();
            test.Dispose();
        }

        private MetricsContextNoOp Create()
        {
            return new MetricsContextNoOp();
        }
    }

    [TestClass]
    public class CounterNoOpTests
    {
        [TestMethod]
        public void Increment_Does_Not_Throw()
        {
            var context = new MetricsContextNoOp();
            var counter = context.Counter("test", Units.Bytes);
            counter.Increment();
        }

        [TestMethod]
        public void Increment_With_Item_Does_Not_Throw()
        {
            var context = new MetricsContextNoOp();
            var counter = context.Counter("test", Units.Bytes);
            counter.Increment("item");
        }

        [TestMethod]
        public void Increment_With_Amount_Does_Not_Throw()
        {
            var context = new MetricsContextNoOp();
            var counter = context.Counter("test", Units.Bytes);
            counter.Increment(5);
        }

        [TestMethod]
        public void Increment_With_Item_And_Amount_Does_Not_Throw()
        {
            var context = new MetricsContextNoOp();
            var counter = context.Counter("test", Units.Bytes);
            counter.Increment("item", 5);
        }

        [TestMethod]
        public void Decrement_Does_Not_Throw()
        {
            var context = new MetricsContextNoOp();
            var counter = context.Counter("test", Units.Bytes);
            counter.Decrement();
        }

        [TestMethod]
        public void Decrement_With_Item_Does_Not_Throw()
        {
            var context = new MetricsContextNoOp();
            var counter = context.Counter("test", Units.Bytes);
            counter.Decrement("item");
        }

        [TestMethod]
        public void Decrement_With_Amount_Does_Not_Throw()
        {
            var context = new MetricsContextNoOp();
            var counter = context.Counter("test", Units.Bytes);
            counter.Decrement(5);
        }

        [TestMethod]
        public void Decrement_With_Item_And_Amount_Does_Not_Throw()
        {
            var context = new MetricsContextNoOp();
            var counter = context.Counter("test", Units.Bytes);
            counter.Decrement("item", 5);
        }
    }

    [TestClass]
    public class MeterNoOpTests
    {
        [TestMethod]
        public void Mark_Does_Not_Throw()
        {
            var context = new MetricsContextNoOp();
            var meter = context.Meter("test", Units.Bytes, TimeUnits.Seconds);
            meter.Mark();
        }

        [TestMethod]
        public void Mark_With_Item_Does_Not_Throw()
        {
            var context = new MetricsContextNoOp();
            var meter = context.Meter("test", Units.Bytes, TimeUnits.Seconds);
            meter.Mark("item");
        }

        [TestMethod]
        public void Mark_With_Count_Does_Not_Throw()
        {
            var context = new MetricsContextNoOp();
            var meter = context.Meter("test", Units.Bytes, TimeUnits.Seconds);
            meter.Mark(5);
        }

        [TestMethod]
        public void Mark_With_Item_And_Count_Does_Not_Throw()
        {
            var context = new MetricsContextNoOp();
            var meter = context.Meter("test", Units.Bytes, TimeUnits.Seconds);
            meter.Mark("item", 5);
        }
    }

    [TestClass]
    public class HistogramNoOpTests
    {
        [TestMethod]
        public void Update_Does_Not_Throw()
        {
            var context = new MetricsContextNoOp();
            var histogram = context.Histogram("test", Units.Bytes);
            histogram.Update(100);
        }

        [TestMethod]
        public void Update_With_UserValue_Does_Not_Throw()
        {
            var context = new MetricsContextNoOp();
            var histogram = context.Histogram("test", Units.Bytes);
            histogram.Update(100, "user");
        }
    }

    [TestClass]
    public class TimerNoOpTests
    {
        [TestMethod]
        public void Record_Does_Not_Throw()
        {
            var context = new MetricsContextNoOp();
            var timer = context.Timer("test", Units.Bytes);
            timer.Record(100, TimeUnits.Milliseconds);
        }

        [TestMethod]
        public void Record_With_UserValue_Does_Not_Throw()
        {
            var context = new MetricsContextNoOp();
            var timer = context.Timer("test", Units.Bytes);
            timer.Record(100, TimeUnits.Milliseconds, "user");
        }

        [TestMethod]
        public void Time_Action_Does_Not_Throw()
        {
            var context = new MetricsContextNoOp();
            var timer = context.Timer("test", Units.Bytes);
            timer.Time(() => { });
        }

        [TestMethod]
        public void Time_Func_Returns_Value()
        {
            var context = new MetricsContextNoOp();
            var timer = context.Timer("test", Units.Bytes);
            var result = timer.Time(() => 42);
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        public void NewContext_Returns_NonNull()
        {
            var context = new MetricsContextNoOp();
            var timer = context.Timer("test", Units.Bytes);
            var timerContext = timer.NewContext();
            Assert.IsNotNull(timerContext);
        }

        [TestMethod]
        public void NewContext_With_UserValue_Returns_NonNull()
        {
            var context = new MetricsContextNoOp();
            var timer = context.Timer("test", Units.Bytes);
            var timerContext = timer.NewContext("myValue");
            Assert.IsNotNull(timerContext);
        }
    }

    [TestClass]
    public class TimerContextNoOpTests
    {
        [TestMethod]
        public void Elapsed_Returns_Zero()
        {
            var context = new MetricsContextNoOp();
            var timer = context.Timer("test", Units.Bytes);
            using (var timerContext = timer.NewContext())
            {
                Assert.AreEqual(TimeSpan.Zero, timerContext.Elapsed);
            }
        }

        [TestMethod]
        public void Dispose_Does_Not_Throw()
        {
            var context = new MetricsContextNoOp();
            var timer = context.Timer("test", Units.Bytes);
            var timerContext = timer.NewContext();
            timerContext.Dispose();
            timerContext.Dispose(); // idempotent
        }
    }
}
