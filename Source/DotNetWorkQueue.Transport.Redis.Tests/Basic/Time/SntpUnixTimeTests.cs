using System;
using DotNetWorkQueue.Transport.Redis.Basic.Time;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Time
{
    [TestClass]
    public class SntpUnixTimeTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = Create();
            Assert.IsNotNull(test);
        }

        [TestMethod]
        public void Name_Returns_SNTP()
        {
            var test = Create();
            Assert.AreEqual("SNTP", test.Name);
        }

        [TestMethod]
        public void DateTimeFromUnixTimestampMilliseconds_Returns_Expected()
        {
            var test = Create();
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var result = test.DateTimeFromUnixTimestampMilliseconds(0);
            Assert.AreEqual(epoch, result);
        }

        [TestMethod]
        public void DateTimeFromUnixTimestampMilliseconds_KnownValue()
        {
            var test = Create();
            // 2020-01-01T00:00:00Z = 1577836800000 ms
            var expected = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var result = test.DateTimeFromUnixTimestampMilliseconds(1577836800000);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void GetCurrentOffset_Default_Is_Zero()
        {
            var test = Create();
            Assert.AreEqual(TimeSpan.Zero, test.GetCurrentOffset);
        }

        private static SntpUnixTime Create()
        {
            var log = Substitute.For<ILogger>();
            var configuration = new SntpTimeConfiguration();
            return new SntpUnixTime(log, configuration);
        }
    }
}
