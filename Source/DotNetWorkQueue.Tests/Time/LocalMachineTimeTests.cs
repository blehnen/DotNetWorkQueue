using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Time;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Time
{
    [TestClass]
    public class LocalMachineTimeTests
    {
        [TestMethod]
        public void Name()
        {
            var test = Create();
            Assert.AreEqual("Local", test.Name);
        }

        [TestMethod]
        public void DateTime_Is_Utc()
        {
            var test = Create();
            var date = test.GetCurrentUtcDate();
            Assert.AreEqual(DateTimeKind.Utc, date.Kind);
        }

        [TestMethod]
        public void Offset_Is_Zero()
        {
            var test = Create();
            test.GetCurrentUtcDate();
            var offSet = test.GetCurrentOffset;
            Assert.AreEqual(TimeSpan.Zero, offSet);
        }

        private static LocalMachineTime Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = new BaseTimeConfiguration();
            return new LocalMachineTime(fixture.Create<ILogger>(), configuration);
        }
    }
}
