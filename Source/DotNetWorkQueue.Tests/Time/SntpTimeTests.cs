// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
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
    public class SntpTimeTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = Create();
            Assert.IsNotNull(test);
        }

        [TestMethod]
        public void Constructor_NullLog_Throws()
        {
            var configuration = new SntpTimeConfiguration();
            Assert.ThrowsExactly<ArgumentNullException>(() => new SntpTime(null, configuration));
        }

        [TestMethod]
        public void Constructor_NullConfiguration_Throws()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            Assert.ThrowsExactly<ArgumentNullException>(() => new SntpTime(fixture.Create<ILogger>(), null));
        }

        [TestMethod]
        public void Name_Returns_SNTP()
        {
            var test = Create();
            Assert.AreEqual("SNTP", test.Name);
        }

        [TestMethod]
        public void GetCurrentOffset_Default_Is_Zero()
        {
            var test = Create();
            Assert.AreEqual(TimeSpan.Zero, test.GetCurrentOffset);
        }

        [TestMethod]
        public void GetCurrentUtcDate_FirstCall_InvokesGetTime_AndReturnsOffsetAdjustedUtc()
        {
            var fake = CreateTestable(
                () => DateTime.UtcNow.AddSeconds(30),
                new SntpTimeConfiguration { RefreshTime = TimeSpan.FromMinutes(5) });

            var before = DateTime.UtcNow;
            var result = fake.GetCurrentUtcDate();
            var after = DateTime.UtcNow;

            Assert.AreEqual(1, fake.GetTimeCallCount);
            Assert.AreEqual(DateTimeKind.Utc, result.Kind);
            Assert.IsGreaterThanOrEqualTo(before.AddSeconds(29), result);
            Assert.IsLessThanOrEqualTo(after.AddSeconds(31), result);
        }

        [TestMethod]
        public void GetCurrentUtcDate_SecondCall_WithinRefreshWindow_UsesCachedOffset()
        {
            var fake = CreateTestable(
                () => DateTime.UtcNow.AddSeconds(30),
                new SntpTimeConfiguration { RefreshTime = TimeSpan.FromMinutes(5) });

            fake.GetCurrentUtcDate();
            fake.GetCurrentUtcDate();

            Assert.AreEqual(1, fake.GetTimeCallCount,
                "Second call within refresh window should reuse cached offset, not re-query.");
        }

        [TestMethod]
        public void GetCurrentUtcDate_AfterRefreshWindow_ReQueries()
        {
            var fake = CreateTestable(
                () => DateTime.UtcNow.AddSeconds(30),
                new SntpTimeConfiguration { RefreshTime = TimeSpan.Zero });

            fake.GetCurrentUtcDate();
            fake.GetCurrentUtcDate();

            Assert.AreEqual(2, fake.GetTimeCallCount,
                "With RefreshTime=Zero, every call should re-query.");
        }

        [TestMethod]
        public void GetCurrentUtcDate_OffsetReflectsTimeProvided()
        {
            var fake = CreateTestable(
                () => DateTime.UtcNow.AddHours(1),
                new SntpTimeConfiguration { RefreshTime = TimeSpan.FromMinutes(5) });

            fake.GetCurrentUtcDate();

            // Offset should be ~1 hour (allow generous slack for test execution time).
            var offset = fake.GetCurrentOffset;
            Assert.IsTrue(offset > TimeSpan.FromMinutes(59) && offset < TimeSpan.FromMinutes(61),
                $"Expected offset near 1 hour but was {offset}");
        }

        private static SntpTime Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = new SntpTimeConfiguration();
            return new SntpTime(fixture.Create<ILogger>(), configuration);
        }

        private static TestableSntpTime CreateTestable(Func<DateTime> getTime, SntpTimeConfiguration configuration)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return new TestableSntpTime(fixture.Create<ILogger>(), configuration, getTime);
        }

        private sealed class TestableSntpTime : SntpTime
        {
            private readonly Func<DateTime> _getTime;

            public TestableSntpTime(ILogger log, SntpTimeConfiguration configuration, Func<DateTime> getTime)
                : base(log, configuration)
            {
                _getTime = getTime;
            }

            public int GetTimeCallCount { get; private set; }

            protected override DateTime GetTime()
            {
                GetTimeCallCount++;
                return _getTime();
            }
        }
    }
}
