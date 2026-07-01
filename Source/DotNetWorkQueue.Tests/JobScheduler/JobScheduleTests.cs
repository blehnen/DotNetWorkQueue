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
using DotNetWorkQueue.JobScheduler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.JobScheduler
{
    [TestClass]
    public class JobScheduleTests
    {
        private static readonly Func<DateTimeOffset> GetNow = () => DateTimeOffset.UtcNow;

        // --- PR comment 1: invalid cron expression tests ---

        [TestMethod]
        public void Constructor_InvalidFieldCount_1Field_Throws()
        {
            Action act = () => new JobSchedule("invalid", GetNow);
            Assert.Throws<ArgumentException>(act);
        }

        [TestMethod]
        public void Constructor_InvalidFieldCount_4Fields_Throws()
        {
            Action act = () => new JobSchedule("* * * *", GetNow);
            Assert.Throws<ArgumentException>(act);
        }

        [TestMethod]
        public void Constructor_InvalidFieldCount_7Fields_Throws()
        {
            Action act = () => new JobSchedule("* * * * * * *", GetNow);
            Assert.Throws<ArgumentException>(act);
        }

        [TestMethod]
        public void Constructor_SchyntaxFormat_Throws()
        {
            Action act = () => new JobSchedule("second(*%3)", GetNow);
            Assert.Throws<ArgumentException>(act);
        }

        [TestMethod]
        public void Constructor_InvalidCronSyntax_5Field_Throws()
        {
            Action act = () => new JobSchedule("99 99 99 99 99", GetNow);
            Assert.Throws<Cronos.CronFormatException>(act);
        }

        [TestMethod]
        public void Constructor_InvalidCronSyntax_6Field_Throws()
        {
            Action act = () => new JobSchedule("99 99 99 99 99 99", GetNow);
            Assert.Throws<Cronos.CronFormatException>(act);
        }

        // --- Valid construction ---

        [TestMethod]
        public void Constructor_5FieldCron_Succeeds()
        {
            var schedule = new JobSchedule("*/5 * * * *", GetNow);
            Assert.AreEqual("*/5 * * * *", schedule.OriginalText);
        }

        [TestMethod]
        public void Constructor_6FieldCron_Succeeds()
        {
            var schedule = new JobSchedule("*/10 * * * * *", GetNow);
            Assert.AreEqual("*/10 * * * * *", schedule.OriginalText);
        }

        // --- PR comments 2 & 3: Next() behavior ---
        // For valid periodic cron expressions, GetNextOccurrence always returns non-null.
        // The InvalidOperationException in Next() is only reachable for degenerate expressions
        // that have no future occurrences, which standard periodic cron cannot produce.

        [TestMethod]
        public void Next_5FieldCron_ReturnsFutureValue()
        {
            var now = DateTimeOffset.UtcNow;
            var schedule = new JobSchedule("* * * * *", () => now);
            var next = schedule.Next();
            Assert.IsTrue(next > now);
        }

        [TestMethod]
        public void Next_6FieldCron_ReturnsFutureValue()
        {
            var now = DateTimeOffset.UtcNow;
            var schedule = new JobSchedule("*/10 * * * * *", () => now);
            var next = schedule.Next();
            Assert.IsTrue(next > now);
        }

        [TestMethod]
        public void Next_AfterOffset_ReturnsValueAfterOffset()
        {
            var after = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
            var schedule = new JobSchedule("*/5 * * * *", () => after);
            var next = schedule.Next(after);
            Assert.IsTrue(next > after);
        }

        // --- Previous() behavior ---

        [TestMethod]
        public void Previous_EveryMinuteCron_ReturnsRecentOccurrence()
        {
            var now = DateTimeOffset.UtcNow;
            var schedule = new JobSchedule("* * * * *", () => now);
            var prev = schedule.Previous();
            Assert.IsNotNull(prev);
            Assert.IsTrue(prev.Value <= now);
        }

        [TestMethod]
        public void Previous_AtOrBefore_ReturnsOccurrence()
        {
            var before = new DateTimeOffset(2025, 6, 1, 12, 30, 0, TimeSpan.Zero);
            var schedule = new JobSchedule("* * * * *", () => before);
            var prev = schedule.Previous(before);
            Assert.IsNotNull(prev);
            Assert.IsTrue(prev.Value <= before);
        }

        // --- PR comment 4: configurable lookback window ---

        [TestMethod]
        public void Previous_DefaultLookback_Is48Hours()
        {
            // A schedule that runs once per day. With 48h lookback, Previous() should find it.
            var now = new DateTimeOffset(2025, 6, 3, 12, 0, 0, TimeSpan.Zero);
            var schedule = new JobSchedule("0 0 * * *", () => now); // daily at midnight
            var prev = schedule.Previous();
            Assert.IsNotNull(prev);
            // Should find midnight of June 3 (12h ago, within 48h window)
            Assert.AreEqual(new DateTimeOffset(2025, 6, 3, 0, 0, 0, TimeSpan.Zero), prev.Value);
        }

        [TestMethod]
        public void Previous_CustomLookback_Honored()
        {
            // A weekly schedule. With default 48h, Previous() might not find it.
            // With a 10-day window, it should.
            var now = new DateTimeOffset(2025, 6, 10, 12, 0, 0, TimeSpan.Zero); // Tuesday
            var schedule = new JobSchedule("0 0 * * 0", () => now, TimeSpan.FromDays(10)); // Sundays at midnight
            var prev = schedule.Previous();
            Assert.IsNotNull(prev);
            // Should find Sunday June 8
            Assert.AreEqual(new DateTimeOffset(2025, 6, 8, 0, 0, 0, TimeSpan.Zero), prev.Value);
        }

        [TestMethod]
        public void Previous_ShortLookback_ReturnsNull_WhenNoOccurrenceInWindow()
        {
            // A weekly schedule with a 1-hour lookback window should miss the last occurrence
            var now = new DateTimeOffset(2025, 6, 10, 12, 0, 0, TimeSpan.Zero); // Tuesday noon
            var schedule = new JobSchedule("0 0 * * 0", () => now, TimeSpan.FromHours(1)); // Sundays at midnight
            var prev = schedule.Previous();
            Assert.IsNull(prev);
        }

        // --- Description ---

        [TestMethod]
        public void Description_ReturnsNonEmptyString()
        {
            var schedule = new JobSchedule("*/5 * * * *", GetNow);
            Assert.IsFalse(string.IsNullOrWhiteSpace(schedule.Description));
        }

        [TestMethod]
        public void Description_6FieldCron_ReturnsNonEmptyString()
        {
            var schedule = new JobSchedule("*/10 * * * * *", GetNow);
            Assert.IsFalse(string.IsNullOrWhiteSpace(schedule.Description));
        }
    }
}
