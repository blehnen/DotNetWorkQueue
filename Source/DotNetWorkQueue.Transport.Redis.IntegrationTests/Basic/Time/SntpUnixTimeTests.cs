using System;
using DotNetWorkQueue.Transport.Redis.Basic.Time;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.Redis.Integration.Tests.Basic.Time
{
    [TestClass]
    public class SntpUnixTimeTests
    {
        [TestMethod]
        public void GetCurrentUnixTimestampMilliseconds_Returns_Value()
        {
            var test = Create();
            var result = test.GetCurrentUnixTimestampMilliseconds();
            Assert.IsTrue(result > 0);
        }

        [TestMethod]
        public void GetCurrentUnixTimestampMilliseconds_Is_Recent()
        {
            var test = Create();
            var result = test.GetCurrentUnixTimestampMilliseconds();

            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var localUnixMs = (long)(DateTime.UtcNow - epoch).TotalMilliseconds;

            // NTP time should be within 5 seconds of local time
            Assert.IsTrue(Math.Abs(result - localUnixMs) < 5000,
                $"NTP time {result} differs from local time {localUnixMs} by more than 5 seconds");
        }

        [TestMethod]
        public void GetCurrentUnixTimestampMilliseconds_Cached_On_Second_Call()
        {
            var test = Create();
            var first = test.GetCurrentUnixTimestampMilliseconds();
            var second = test.GetCurrentUnixTimestampMilliseconds();

            // Second call should use cached offset, so results should be very close
            Assert.IsTrue(Math.Abs(second - first) < 1000,
                $"First call {first} and second call {second} differ by more than 1 second");
        }

        [TestMethod]
        public void GetAddDifferenceMilliseconds_Adds_Offset()
        {
            var test = Create();
            var current = test.GetCurrentUnixTimestampMilliseconds();
            var difference = TimeSpan.FromMinutes(5);
            var result = test.GetAddDifferenceMilliseconds(difference);

            var expectedDifferenceMs = (long)difference.TotalMilliseconds;
            // Allow small tolerance for elapsed time between calls
            Assert.IsTrue(Math.Abs(result - current - expectedDifferenceMs) < 1000,
                $"Expected ~{expectedDifferenceMs}ms difference, got {result - current}ms");
        }

        [TestMethod]
        public void GetSubtractDifferenceMilliseconds_Subtracts_Offset()
        {
            var test = Create();
            var current = test.GetCurrentUnixTimestampMilliseconds();
            var difference = TimeSpan.FromMinutes(5);
            var result = test.GetSubtractDifferenceMilliseconds(difference);

            var expectedDifferenceMs = (long)difference.TotalMilliseconds;
            // Allow small tolerance for elapsed time between calls
            Assert.IsTrue(Math.Abs(current - result - expectedDifferenceMs) < 1000,
                $"Expected ~{expectedDifferenceMs}ms difference, got {current - result}ms");
        }

        [TestMethod]
        public void GetCurrentUtcDate_Returns_Recent_Date()
        {
            var test = Create();
            var result = test.GetCurrentUtcDate();

            // Should be within 5 seconds of local UTC time
            var diff = Math.Abs((result - DateTime.UtcNow).TotalSeconds);
            Assert.IsTrue(diff < 5,
                $"NTP date differs from local UTC by {diff} seconds");
        }

        private static SntpUnixTime Create()
        {
            var log = Substitute.For<ILogger>();
            var configuration = new SntpTimeConfiguration();
            return new SntpUnixTime(log, configuration);
        }
    }
}
