using DotNetWorkQueue.Metrics.NoOp;
using Xunit;

namespace DotNetWorkQueue.Tests.Metrics.NoOp
{
    public class MetricsNoOpTests
    {
        [Fact]
        public void Test_Context()
        {
            var test = Create();
            var result = test.Context("test");
            Assert.NotNull(result);
        }

        [Fact]
        public void Test_ShutdownContext()
        {
            var test = Create();
            test.ShutdownContext("test");
        }

        [Fact]
        public void Test_Gauge()
        {
            var test = Create();
            test.Gauge("test", () => 0.0d, Units.Bytes, null);
        }

        [Fact]
        public void Test_Meter()
        {
            var test = Create();
            var result = test.Meter("test", Units.Bytes, TimeUnits.Days);
            Assert.NotNull(result);
        }

        [Fact]
        public void Test_Meter2()
        {
            var test = Create();
            var result = test.Meter("test", "Bytes", TimeUnits.Days);
            Assert.NotNull(result);
        }

        [Fact]
        public void Test_Counter()
        {
            var test = Create();
            var result = test.Counter("test", Units.Bytes);
            Assert.NotNull(result);
        }

        [Fact]
        public void Test_Counter2()
        {
            var test = Create();
            var result = test.Counter("test", "Bytes");
            Assert.NotNull(result);
        }

        [Fact]
        public void Test_Histogram()
        {
            var test = Create();
            var result = test.Histogram("testing", Units.Bytes, SamplingTypes.LongTerm);
            Assert.NotNull(result);
        }

        [Fact]
        public void Test_Timer()
        {
            var test = Create();
            var result = test.Timer("Testing", Units.Bytes, SamplingTypes.LongTerm, TimeUnits.Minutes, TimeUnits.Milliseconds);
            Assert.NotNull(result);
        }

        [Fact]
        public void Test_Dispose()
        {
            var test = Create();
            test.Dispose();
        }

        [Fact]
        public void Test_CollectedMetrics()
        {
            var test = Create();
            Assert.Null(test.CollectedMetrics);
        }

        private MetricsNoOp Create()
        {
            return new MetricsNoOp();
        }
    }
}
