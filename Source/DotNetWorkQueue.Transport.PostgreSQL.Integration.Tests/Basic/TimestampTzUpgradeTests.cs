using System;
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Basic
{
    /// <summary>
    /// Schema version 2: the timestamp to timestamptz conversion (GitHub #311, #374).
    ///
    /// The same job docs/upgrade/0.12.0/postgresql.sql does by hand in its step 2, done by the
    /// library instead - so an existing queue no longer needs a script edited per queue.
    /// </summary>
    [TestClass]
    [Retry(1)]
    public class TimestampTzUpgradeTests
    {
        /// <summary>
        /// The zone the imaginary application wrote from. Fixed rather than the machine's, so the
        /// test means the same thing everywhere, and deliberately not UTC - a conversion that read
        /// the value as UTC would pass against UTC while being wrong for everyone else.
        /// </summary>
        private const string WriterZone = "America/Chicago";

        /// <summary>The instant the message was enqueued at, as the application meant it.</summary>
        private const string EnqueuedInstant = "2001-02-03 04:05:06+00";

        /// <summary>The same instant in the writer's zone, which is what 0.11.0 actually stored.</summary>
        private const string StoredByOldVersions = "2001-02-02 22:05:06";

        [TestMethod]
        public void AQueueWithNaiveTimestamps_IsConvertedKeepingTheInstant()
        {
            using var harness = NewQueue(declareTimeZone: true);
            MakeLookLikeAPre0120Queue(harness);

            Assert.AreEqual("timestamp without time zone", ColumnType(harness, "History", "enqueuedutc"),
                "setup did not put the column back, so there is nothing to convert");
            Assert.AreEqual(StoredByOldVersions, harness.Text($"SELECT EnqueuedUtc::text FROM {harness.QueueName}History"),
                "the stored value is not what 0.11.0 would have written");

            var result = harness.Updater.UpgradeSchema();

            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, result.Status, result.ErrorMessage);
            Assert.AreEqual(2, result.EndingVersion);

            Assert.AreEqual("timestamp with time zone", ColumnType(harness, "History", "enqueuedutc"));
            Assert.AreEqual("timestamp with time zone", ColumnType(harness, "MetaData", "queueddatetime"));

            //the point of the whole thing: the same instant, not the same digits
            Assert.AreEqual(EnqueuedInstant,
                harness.Text($"SELECT EnqueuedUtc AT TIME ZONE 'UTC' || '+00' FROM {harness.QueueName}History"),
                "the conversion shifted the instant");
        }

        [TestMethod]
        public void WithoutADeclaredTimeZone_TheUpgradeRefusesRatherThanGuessing()
        {
            //nothing in the database records the zone the old values were written in. Assuming UTC
            //would silently shift every timestamp on any deployment that was not in UTC.
            using var harness = NewQueue(declareTimeZone: false);
            MakeLookLikeAPre0120Queue(harness);

            var result = harness.Updater.UpgradeSchema();

            Assert.AreEqual(SchemaUpgradeStatus.Failed, result.Status);
            Assert.Contains("SetUpgradeSourceTimeZone", result.ErrorMessage);

            //and it changed nothing on the way out
            Assert.AreEqual("timestamp without time zone", ColumnType(harness, "History", "enqueuedutc"));
            Assert.AreEqual(StoredByOldVersions, harness.Text($"SELECT EnqueuedUtc::text FROM {harness.QueueName}History"));
        }

        [TestMethod]
        public void AQueueAlreadyOnTimestampTz_NeedsNoTimeZoneAndIsUpgraded()
        {
            //a queue created after 0.12.0 but before schema versioning: the columns are right, only
            //the version table is missing. It must not be made to declare a zone it does not need.
            using var harness = NewQueue(declareTimeZone: false);
            harness.DropSchemaVersionTable();

            Assert.AreEqual("timestamp with time zone", ColumnType(harness, "History", "enqueuedutc"));

            var result = harness.Updater.UpgradeSchema();

            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, result.Status, result.ErrorMessage);
            Assert.AreEqual(2, harness.Updater.CurrentSchemaVersion);
        }

        [TestMethod]
        public void UpgradingTwice_ConvertsOnceAndThenReportsCurrent()
        {
            using var harness = NewQueue(declareTimeZone: true);
            MakeLookLikeAPre0120Queue(harness);

            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, harness.Updater.UpgradeSchema().Status);

            //converting an already converted column is an error, so a second run has to notice
            var second = harness.Updater.UpgradeSchema();
            Assert.AreEqual(SchemaUpgradeStatus.AlreadyCurrent, second.Status, second.ErrorMessage);
        }

        /// <summary>
        /// A queue with history on, and the schema version the conversion needs declared or not.
        /// </summary>
        /// <remarks>
        /// History is off by default and is the table this matters most on - it keeps values long
        /// enough for a shifted timestamp to be noticed.
        /// </remarks>
        private static PostgreSqlQueueHarness NewQueue(bool declareTimeZone)
        {
            var settings = new Dictionary<string, string>();
            if (declareTimeZone)
            {
                settings.SetUpgradeSourceTimeZone(WriterZone);
            }

            return new PostgreSqlQueueHarness(
                connectionSettings: settings,
                beforeCreate: creation => creation.Options.EnableHistory = true);
        }

        /// <summary>
        /// Leaves the queue as 0.11.0 left it: naive timestamp columns holding local time, and no
        /// version table. The unique index stays, so version 1 has nothing to do and this is a test
        /// of version 2 rather than of both.
        /// </summary>
        private static void MakeLookLikeAPre0120Queue(PostgreSqlQueueHarness harness)
        {
            harness.DropSchemaVersionTable();

            harness.Execute($@"INSERT INTO {harness.QueueName}History (QueueID, Status, EnqueuedUtc, RetryCount)
                               VALUES ('a-message', 0, timestamptz '{EnqueuedInstant}', 0)");

            //converted with the session in the writer's zone, which is how the value became local
            harness.Execute($@"
                SET TIME ZONE '{WriterZone}';
                ALTER TABLE {harness.QueueName}History ALTER COLUMN EnqueuedUtc  TYPE timestamp;
                ALTER TABLE {harness.QueueName}History ALTER COLUMN StartedUtc   TYPE timestamp;
                ALTER TABLE {harness.QueueName}History ALTER COLUMN CompletedUtc TYPE timestamp;
                ALTER TABLE {harness.QueueName}MetaData ALTER COLUMN QueuedDateTime TYPE timestamp;
                ALTER TABLE {harness.QueueName}MetaDataErrors ALTER COLUMN QueuedDateTime TYPE timestamp;
                ALTER TABLE {harness.QueueName}MetaDataErrors ALTER COLUMN LastExceptionDate TYPE timestamp;");
        }

        /// <summary>
        /// The declared type of a column, read from the catalog.
        /// </summary>
        /// <remarks>
        /// The table and column are bound rather than pasted in. They are values here, not
        /// identifiers - to_regclass takes a name as a string - so binding them is available and is
        /// what the rest of the library does with a catalog look-up.
        /// </remarks>
        private static string ColumnType(PostgreSqlQueueHarness harness, string tableSuffix, string column) =>
            harness.Text(@"SELECT format_type(a.atttypid, a.atttypmod) FROM pg_attribute a
                           WHERE a.attrelid = to_regclass(@Table)
                             AND lower(a.attname) = lower(@Column) AND a.attnum > 0 AND NOT a.attisdropped",
                ("@Table", harness.QueueName + tableSuffix), ("@Column", column));
    }
}
