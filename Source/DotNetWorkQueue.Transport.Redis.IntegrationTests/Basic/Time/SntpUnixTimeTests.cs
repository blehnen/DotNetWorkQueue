using System;
using DotNetWorkQueue.Transport.Redis.Basic.Time;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.Redis.Integration.Tests.Basic.Time
{
    /// <summary>
    /// Exercises the SNTP time client against a live NTP server (default pool.ntp.org).
    /// </summary>
    /// <remarks>
    /// These tests reach the public NTP pool over UDP, so they depend on outbound network access.
    /// A single <see cref="SntpUnixTime"/> instance is shared across every test via <see cref="ClassInit"/>
    /// so the offset is fetched once (the client caches it) instead of issuing a burst of queries that a
    /// public pool may rate-limit or drop. Tagged <c>ExternalNetwork</c> so a host with unreliable outbound
    /// NTP can exclude them via <c>--filter "TestCategory!=ExternalNetwork"</c>.
    /// </remarks>
    [TestClass]
    [TestCategory("ExternalNetwork")]
    public class SntpUnixTimeTests
    {
        private static SntpUnixTime _test;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            var log = Substitute.For<ILogger>();
            var configuration = new SntpTimeConfiguration();
            _test = new SntpUnixTime(log, configuration);
        }

        [TestMethod]
        public void GetCurrentUnixTimestampMilliseconds_Returns_Value()
        {
            var result = _test.GetCurrentUnixTimestampMilliseconds();
            Assert.IsGreaterThan(0, result);
        }

        [TestMethod]
        public void GetCurrentUnixTimestampMilliseconds_Is_Recent()
        {
            var result = _test.GetCurrentUnixTimestampMilliseconds();

            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var localUnixMs = (long)(DateTime.UtcNow - epoch).TotalMilliseconds;

            // NTP time should be within 5 seconds of local time
            Assert.IsLessThan(5000, Math.Abs(result - localUnixMs),
                $"NTP time {result} differs from local time {localUnixMs} by more than 5 seconds");
        }

        [TestMethod]
        public void GetCurrentUnixTimestampMilliseconds_Cached_On_Second_Call()
        {
            var first = _test.GetCurrentUnixTimestampMilliseconds();
            var second = _test.GetCurrentUnixTimestampMilliseconds();

            // Second call should use cached offset, so results should be very close
            Assert.IsLessThan(1000, Math.Abs(second - first),
                $"First call {first} and second call {second} differ by more than 1 second");
        }

        [TestMethod]
        public void GetAddDifferenceMilliseconds_Adds_Offset()
        {
            var current = _test.GetCurrentUnixTimestampMilliseconds();
            var difference = TimeSpan.FromMinutes(5);
            var result = _test.GetAddDifferenceMilliseconds(difference);

            var expectedDifferenceMs = (long)difference.TotalMilliseconds;
            // Allow small tolerance for elapsed time between calls
            Assert.IsLessThan(1000, Math.Abs(result - current - expectedDifferenceMs),
                $"Expected ~{expectedDifferenceMs}ms difference, got {result - current}ms");
        }

        [TestMethod]
        public void GetSubtractDifferenceMilliseconds_Subtracts_Offset()
        {
            var current = _test.GetCurrentUnixTimestampMilliseconds();
            var difference = TimeSpan.FromMinutes(5);
            var result = _test.GetSubtractDifferenceMilliseconds(difference);

            var expectedDifferenceMs = (long)difference.TotalMilliseconds;
            // Allow small tolerance for elapsed time between calls
            Assert.IsLessThan(1000, Math.Abs(current - result - expectedDifferenceMs),
                $"Expected ~{expectedDifferenceMs}ms difference, got {current - result}ms");
        }

        [TestMethod]
        public void GetCurrentUtcDate_Returns_Recent_Date()
        {
            var result = _test.GetCurrentUtcDate();

            // Should be within 5 seconds of local UTC time
            var diff = Math.Abs((result - DateTime.UtcNow).TotalSeconds);
            Assert.IsLessThan(5, diff,
                $"NTP date differs from local UTC by {diff} seconds");
        }
    }
}
