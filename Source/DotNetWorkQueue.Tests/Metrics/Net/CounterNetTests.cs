using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using DotNetWorkQueue.Metrics.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Metrics.Net
{
    [TestClass]
    public class CounterNetTests
    {
        [TestMethod]
        public void Value_Starts_At_Zero()
        {
            var test = Create();
            Assert.AreEqual(0, test.Value);
        }

        [TestMethod]
        public void Increment_Increases_Value_By_One()
        {
            var test = Create();
            test.Increment();
            Assert.AreEqual(1, test.Value);
        }

        [TestMethod]
        public void Increment_With_Amount_Increases_Value()
        {
            var test = Create();
            test.Increment(5);
            Assert.AreEqual(5, test.Value);
        }

        [TestMethod]
        public void Increment_With_Item_Increases_Value()
        {
            var test = Create();
            test.Increment("item1");
            Assert.AreEqual(1, test.Value);
        }

        [TestMethod]
        public void Increment_With_Item_And_Amount_Increases_Value()
        {
            var test = Create();
            test.Increment("item1", 10);
            Assert.AreEqual(10, test.Value);
        }

        [TestMethod]
        public void Decrement_Decreases_Value_By_One()
        {
            var test = Create();
            test.Increment(5);
            test.Decrement();
            Assert.AreEqual(4, test.Value);
        }

        [TestMethod]
        public void Decrement_With_Amount_Decreases_Value()
        {
            var test = Create();
            test.Increment(10);
            test.Decrement(3);
            Assert.AreEqual(7, test.Value);
        }

        [TestMethod]
        public void Decrement_With_Item_Decreases_Value()
        {
            var test = Create();
            test.Increment(5);
            test.Decrement("item1");
            Assert.AreEqual(4, test.Value);
        }

        [TestMethod]
        public void Decrement_With_Item_And_Amount_Decreases_Value()
        {
            var test = Create();
            test.Increment(10);
            test.Decrement("item1", 7);
            Assert.AreEqual(3, test.Value);
        }

        private CounterNet Create()
        {
            var meter = new Meter("TestMeter." + Guid.NewGuid());
            var counter = meter.CreateUpDownCounter<long>("test_counter");
            return new CounterNet(counter, Array.Empty<KeyValuePair<string, object>>());
        }
    }
}
