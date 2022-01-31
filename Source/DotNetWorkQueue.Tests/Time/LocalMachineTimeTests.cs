using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Time;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DotNetWorkQueue.Tests.Time
{
    public class LocalMachineTimeTests
    {
        [Fact]
        public void Name()
        {
            var test = Create();
            Assert.Equal("Local", test.Name);
        }

        [Fact]
        public void DateTime_Is_Utc()
        {
            var test = Create();
            var date = test.GetCurrentUtcDate();
            Assert.Equal(DateTimeKind.Utc, date.Kind);
        }

        [Fact]
        public void Offset_Is_Zero()
        {
            var test = Create();
            test.GetCurrentUtcDate();
            var offSet = test.GetCurrentOffset;
            Assert.Equal(TimeSpan.Zero, offSet);
        }

        private static LocalMachineTime Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = new BaseTimeConfiguration();
            return new LocalMachineTime(fixture.Create<ILogger>(), configuration);
        }
    }
}
