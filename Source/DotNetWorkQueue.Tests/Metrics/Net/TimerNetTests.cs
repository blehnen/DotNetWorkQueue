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

        //NewContext hands back a shared do-nothing scope when nothing is collecting this
        //instrument: it is called for every message sent and consumed, and the measurement would
        //only be discarded. These two pin both halves of that.

        [TestMethod]
        public void NewContext_Does_Not_Measure_When_Nothing_Is_Listening()
        {
            var test = Create();
            using var context = test.NewContext();
            System.Threading.Thread.Sleep(5);
            Assert.AreEqual(TimeSpan.Zero, context.Elapsed);
        }

        [TestMethod]
        public void NewContext_Records_When_Something_Is_Listening()
        {
            var meter = new Meter("TestMeter." + Guid.NewGuid());
            var histogram = meter.CreateHistogram<double>("test_timer", "ms");

            var recorded = new List<double>();
            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, l) =>
            {
                if (ReferenceEquals(instrument, histogram)) l.EnableMeasurementEvents(instrument);
            };
            listener.SetMeasurementEventCallback<double>((_, measurement, _, _) => recorded.Add(measurement));
            listener.Start();

            var test = new TimerNet(histogram, Array.Empty<KeyValuePair<string, object>>());
            using (var context = test.NewContext())
            {
                System.Threading.Thread.Sleep(5);
                Assert.IsTrue(context.Elapsed > TimeSpan.Zero, "the scope should be timing");
            }

            listener.Dispose();
            Assert.HasCount(1, recorded);
            Assert.IsTrue(recorded[0] > 0, "the recorded duration should be greater than zero");
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
