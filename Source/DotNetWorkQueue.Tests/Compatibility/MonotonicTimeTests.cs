using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetWorkQueue.Compatibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Compatibility
{
    /// <summary>
    /// <see cref="MonotonicTime"/> stands in for <c>Stopwatch.GetElapsedTime</c> on the targets
    /// whose framework does not have it (GitHub #252).
    /// </summary>
    [TestClass]
    public class MonotonicTimeTests
    {
        [TestMethod]
        public async Task It_Converts_Timestamps_To_The_Same_Span_The_Framework_Does()
        {
            //the arithmetic is the whole type, so it is checked against the implementation it
            //replaces rather than against a restatement of itself
            var start = Stopwatch.GetTimestamp();
            await Task.Delay(20);
            var end = Stopwatch.GetTimestamp();

            Assert.AreEqual(Stopwatch.GetElapsedTime(start, end), MonotonicTime.Elapsed(start, end));
        }

        [TestMethod]
        public void Two_Identical_Timestamps_Are_No_Time_At_All()
        {
            var now = Stopwatch.GetTimestamp();

            Assert.AreEqual(TimeSpan.Zero, MonotonicTime.Elapsed(now, now));
        }

        [TestMethod]
        public async Task It_Measures_A_Real_Delay()
        {
            var start = Stopwatch.GetTimestamp();
            await Task.Delay(50);

            var elapsed = MonotonicTime.ElapsedSince(start);

            //a conversion using the wrong scale is out by orders of magnitude, not by jitter
            Assert.IsGreaterThanOrEqualTo(40d, elapsed.TotalMilliseconds);
            Assert.IsLessThanOrEqualTo(5000d, elapsed.TotalMilliseconds);
        }

        [TestMethod]
        public void A_Timestamp_In_The_Future_Gives_A_Negative_Span()
        {
            //same as the framework: the caller passed them the wrong way round, and saying so with
            //a negative result beats silently reporting zero
            var now = Stopwatch.GetTimestamp();

            Assert.IsLessThanOrEqualTo(TimeSpan.Zero, MonotonicTime.Elapsed(now + Stopwatch.Frequency, now));
        }
    }
}
