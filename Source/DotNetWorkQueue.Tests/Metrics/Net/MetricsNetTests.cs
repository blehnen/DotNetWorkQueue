using System.Collections.Generic;
using DotNetWorkQueue.Metrics.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Metrics.Net
{
    [TestClass]
    public class MetricsNetTests
    {
        [TestMethod]
        public void Context_Returns_Instance()
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
        public void ShutdownContext_NonExistent_Does_Not_Throw()
        {
            var test = Create();
            test.ShutdownContext("doesnotexist");
        }

        [TestMethod]
        public void Counter_With_Units_Returns_Instance()
        {
            var test = Create();
            var result = test.Counter("test-counter", Units.Items);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Counter_With_Units_Returns_Same_Instance_For_Same_Name()
        {
            var test = Create();
            var first = test.Counter("test-counter", Units.Items);
            var second = test.Counter("test-counter", Units.Items);
            Assert.AreSame(first, second);
        }

        [TestMethod]
        public void Counter_With_Units_Returns_Different_Instance_For_Different_Name()
        {
            var test = Create();
            var first = test.Counter("counter1", Units.Items);
            var second = test.Counter("counter2", Units.Items);
            Assert.AreNotSame(first, second);
        }

        [TestMethod]
        public void Counter_With_String_Unit_Returns_Instance()
        {
            var test = Create();
            var result = test.Counter("test-counter-str", "Items");
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Counter_With_String_Unit_Returns_Same_Instance_For_Same_Name()
        {
            var test = Create();
            var first = test.Counter("test-counter-str", "Items");
            var second = test.Counter("test-counter-str", "Items");
            Assert.AreSame(first, second);
        }

        [TestMethod]
        public void Counter_With_Tags_Returns_Instance()
        {
            var test = Create();
            var tags = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("key", "value")
            };
            var result = test.Counter("tagged-counter", Units.Items, tags);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Counter_With_String_Unit_And_Tags_Returns_Instance()
        {
            var test = Create();
            var tags = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("key", "value")
            };
            var result = test.Counter("tagged-counter-str", "Items", tags);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Meter_With_Units_Returns_Instance()
        {
            var test = Create();
            var result = test.Meter("test-meter", Units.Items, TimeUnits.Seconds);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Meter_With_Units_Returns_Same_Instance_For_Same_Name()
        {
            var test = Create();
            var first = test.Meter("test-meter", Units.Items, TimeUnits.Seconds);
            var second = test.Meter("test-meter", Units.Items, TimeUnits.Seconds);
            Assert.AreSame(first, second);
        }

        [TestMethod]
        public void Meter_With_String_Unit_Returns_Instance()
        {
            var test = Create();
            var result = test.Meter("test-meter-str", "Items", TimeUnits.Seconds);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Meter_With_String_Unit_Returns_Same_Instance_For_Same_Name()
        {
            var test = Create();
            var first = test.Meter("test-meter-str", "Items", TimeUnits.Seconds);
            var second = test.Meter("test-meter-str", "Items", TimeUnits.Seconds);
            Assert.AreSame(first, second);
        }

        [TestMethod]
        public void Meter_With_Tags_Returns_Instance()
        {
            var test = Create();
            var tags = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("key", "value")
            };
            var result = test.Meter("tagged-meter", Units.Items, TimeUnits.Seconds, tags);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Meter_With_String_Unit_And_Tags_Returns_Instance()
        {
            var test = Create();
            var tags = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("key", "value")
            };
            var result = test.Meter("tagged-meter-str", "Items", TimeUnits.Seconds, tags);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Histogram_Returns_Instance()
        {
            var test = Create();
            var result = test.Histogram("test-histogram", Units.Items);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Histogram_With_Tags_Returns_Instance()
        {
            var test = Create();
            var tags = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("key", "value")
            };
            var result = test.Histogram("tagged-histogram", Units.Items, tags);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Timer_Returns_Instance()
        {
            var test = Create();
            var result = test.Timer("test-timer", Units.Items, TimeUnits.Seconds, TimeUnits.Milliseconds);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Timer_With_Tags_Returns_Instance()
        {
            var test = Create();
            var tags = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("key", "value")
            };
            var result = test.Timer("tagged-timer", Units.Items, TimeUnits.Seconds, TimeUnits.Milliseconds, tags);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Gauge_Does_Not_Throw()
        {
            var test = Create();
            test.Gauge("test-gauge", () => 42.0, Units.None);
        }

        [TestMethod]
        public void Gauge_With_Tags_Does_Not_Throw()
        {
            var test = Create();
            var tags = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("key", "value")
            };
            test.Gauge("tagged-gauge", () => 42.0, Units.None, tags);
        }

        [TestMethod]
        public void GetCollectedMetrics_Empty_Returns_Empty_Snapshot()
        {
            var test = Create();
            var snapshot = test.GetCollectedMetrics();
            Assert.IsNotNull(snapshot);
            Assert.IsEmpty(snapshot.Counters);
            Assert.IsEmpty(snapshot.Meters);
        }

        [TestMethod]
        public void GetCollectedMetrics_Returns_Counter_Values()
        {
            var test = Create();
            var counter = test.Counter("my-counter", Units.Items);
            counter.Increment();
            counter.Increment();

            var snapshot = test.GetCollectedMetrics();
            Assert.IsTrue(snapshot.Counters.ContainsKey("my-counter"));
            Assert.AreEqual(2, snapshot.Counters["my-counter"]);
        }

        [TestMethod]
        public void GetCollectedMetrics_Returns_Meter_Values()
        {
            var test = Create();
            var meter = test.Meter("my-meter", Units.Items, TimeUnits.Seconds);
            meter.Mark();
            meter.Mark();
            meter.Mark();

            var snapshot = test.GetCollectedMetrics();
            Assert.IsTrue(snapshot.Meters.ContainsKey("my-meter"));
            Assert.AreEqual(3, snapshot.Meters["my-meter"]);
        }

        [TestMethod]
        public void GetCollectedMetrics_Returns_Multiple_Counters_And_Meters()
        {
            var test = Create();
            var counter1 = test.Counter("counter-a", Units.Items);
            var counter2 = test.Counter("counter-b", Units.Items);
            var meter1 = test.Meter("meter-a", Units.Items, TimeUnits.Seconds);

            counter1.Increment();
            counter2.Increment(5);
            meter1.Mark();

            var snapshot = test.GetCollectedMetrics();
            Assert.HasCount(2, snapshot.Counters);
            Assert.AreEqual(1, snapshot.Counters["counter-a"]);
            Assert.AreEqual(5, snapshot.Counters["counter-b"]);
            Assert.HasCount(1, snapshot.Meters);
            Assert.AreEqual(1, snapshot.Meters["meter-a"]);
        }

        [TestMethod]
        public void Dispose_Does_Not_Throw()
        {
            var test = Create();
            test.Dispose();
        }

        [TestMethod]
        public void Dispose_With_Child_Contexts_Does_Not_Throw()
        {
            var test = Create();
            test.Context("child1");
            test.Context("child2");
            test.Dispose();
        }

        private MetricsNet Create()
        {
            return new MetricsNet();
        }
    }
}
