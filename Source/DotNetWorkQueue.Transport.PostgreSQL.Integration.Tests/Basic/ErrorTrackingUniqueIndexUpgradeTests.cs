using System;
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Npgsql;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Basic
{
    /// <summary>
    /// Schema version 1 on PostgreSQL: the unique index on the error tracking table's
    /// (QueueID, ExceptionType), and the collapse of the duplicate rows that its absence allowed.
    /// </summary>
    /// <remarks>
    /// The SQLite suite covers the same version, but the statements are not the same statements: the
    /// index DDL is written per transport, and the de-duplication correlates a subquery against the
    /// table being updated, which every dialect spells its own way. Running it here is the only thing
    /// that shows it parses and means what it should on PostgreSQL (GitHub #308).
    /// </remarks>
    [TestClass]
    [Retry(1)]
    public class ErrorTrackingUniqueIndexUpgradeTests
    {
        [TestMethod]
        public void AQueueFromBeforeTheIndex_GetsItAndCollapsesTheDuplicatesItAllowed()
        {
            using var harness = new PostgreSqlQueueHarness();
            MakeLookLikeAPreIndexQueue(harness);

            Assert.IsFalse(UniqueIndexExists(harness), "setup did not remove the index");
            Assert.AreEqual(0, harness.Updater.CurrentSchemaVersion);

            //two rows for one pair is what the check-then-write path leaves behind under a race. Each
            //count is an absolute total, so the pair collapses to the larger rather than to their sum.
            InsertErrorRow(harness, 1, "System.TimeoutException", 3);
            InsertErrorRow(harness, 1, "System.TimeoutException", 4);
            //and rows that are not duplicates have to survive untouched
            InsertErrorRow(harness, 1, "System.InvalidOperationException", 2);
            InsertErrorRow(harness, 2, "System.TimeoutException", 7);

            var result = harness.Updater.UpgradeSchema();

            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, result.Status, result.ErrorMessage);
            //the target rather than a literal: PostgreSQL has a version 2 as well, and this test is
            //about version 1 having run, not about how many there are
            Assert.AreEqual(harness.Updater.TargetSchemaVersion, result.EndingVersion);
            Assert.IsTrue(UniqueIndexExists(harness), "the upgrade did not create the index");
            Assert.AreEqual(harness.Updater.TargetSchemaVersion, harness.Updater.CurrentSchemaVersion);

            var rows = ReadErrorRows(harness);
            Assert.HasCount(3, rows, "de-duplication removed rows that were not duplicates");
            Assert.AreEqual(4, RetryCountFor(rows, 1, "System.TimeoutException"),
                "the surviving row must carry the largest count rather than the sum");
            Assert.AreEqual(2, RetryCountFor(rows, 1, "System.InvalidOperationException"));
            Assert.AreEqual(7, RetryCountFor(rows, 2, "System.TimeoutException"));
        }

        [TestMethod]
        public void AQueueThatAlreadyHasTheIndex_UpgradesInsteadOfFailingOnIt()
        {
            //a queue created after #299 but before schema versioning: the index is there, the version
            //table is not. Creating the index again would fail and take the whole upgrade with it.
            using var harness = new PostgreSqlQueueHarness();
            harness.DropSchemaVersionTable();

            Assert.IsTrue(UniqueIndexExists(harness), "expected a newly created queue to carry the index");

            var result = harness.Updater.UpgradeSchema();

            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, result.Status, result.ErrorMessage);
            Assert.AreEqual(harness.Updater.TargetSchemaVersion, harness.Updater.CurrentSchemaVersion);
            Assert.IsTrue(UniqueIndexExists(harness));
        }

        [TestMethod]
        public void AfterTheUpgrade_ASecondRowForTheSamePairIsRefused()
        {
            using var harness = new PostgreSqlQueueHarness();
            MakeLookLikeAPreIndexQueue(harness);
            InsertErrorRow(harness, 1, "System.TimeoutException", 1);

            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, harness.Updater.UpgradeSchema().Status);

            //without the index this insert succeeds, the retry count reads low, and the message gets
            //more attempts than it was configured for
            Assert.ThrowsExactly<PostgresException>(
                () => InsertErrorRow(harness, 1, "System.TimeoutException", 2));
        }

        [TestMethod]
        public void TwoQueuesWhoseIndexNamesWouldTruncateAlike_BothUpgrade()
        {
            //PostgreSQL truncates identifiers at 63 bytes and index names are unique per schema, so
            //"IX_QueueIDExceptionType" + table runs out of room: two queue names matching for their
            //first 40 characters produce one identifier. Both queues are already here - they predate
            //the index, which is why there is anything to upgrade - so the second cannot be renamed
            //out of the way, and without a shorter name its upgrade fails with 42P07 and leaves it
            //refusing producers for good.
            var shared = "u" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var prefix = (shared + new string('x', 40)).Substring(0, 40);
            Assert.AreEqual(40, prefix.Length);

            //the second queue has to be created after the first one's index is out of the way. Today
            //creating both is impossible: the second collides on the index and the failure is reported
            //as "already exists", which carries Success - so the caller is told it has a queue that was
            //rolled back. That is the creation path's own bug and is not what this test is about; the
            //pair being upgraded here is one that only a pre-index version of the library could leave
            //behind, which is exactly the population an upgrade has to cope with.
            using var first = new PostgreSqlQueueHarness(prefix + "aaa");
            MakeLookLikeAPreIndexQueue(first);

            using var second = new PostgreSqlQueueHarness(prefix + "bbb");
            MakeLookLikeAPreIndexQueue(second);

            //the names differ, but not within the 40 characters that survive truncation
            Assert.AreNotEqual(first.QueueName, second.QueueName);
            Assert.AreEqual(first.QueueName.Substring(0, 40), second.QueueName.Substring(0, 40));

            var firstResult = first.Updater.UpgradeSchema();
            var secondResult = second.Updater.UpgradeSchema();

            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, firstResult.Status, firstResult.ErrorMessage);
            Assert.AreEqual(SchemaUpgradeStatus.Upgraded, secondResult.Status, secondResult.ErrorMessage);
            Assert.IsTrue(UniqueIndexExists(first));
            Assert.IsTrue(UniqueIndexExists(second));
        }

        /// <summary>The error tracking table, named as this queue's connection addresses it.</summary>
        /// <remarks>
        /// From the helper rather than built here, so the name carries whatever schema the
        /// connection is configured for.
        /// </remarks>
        private static string ErrorTrackingTable(PostgreSqlQueueHarness harness) =>
            harness.Resolve<ITableNameHelper>().ErrorTrackingName;

        /// <summary>
        /// Leaves the queue as one created before #299: no unique index, no version table.
        /// </summary>
        private static void MakeLookLikeAPreIndexQueue(PostgreSqlQueueHarness harness)
        {
            harness.DropSchemaVersionTable();
            var name = UniqueIndexName(harness);
            Assert.IsNotNull(name, "a newly created queue should have carried the index");
            harness.Execute($"drop index {name}");
        }

        private static bool UniqueIndexExists(PostgreSqlQueueHarness harness) =>
            UniqueIndexName(harness) != null;

        /// <summary>
        /// The unique index over (QueueID, ExceptionType), found by its shape rather than its name -
        /// which is how the library looks for it, and the only way to tell an index that carries the
        /// constraint from one that merely includes the columns.
        /// </summary>
        private static string UniqueIndexName(PostgreSqlQueueHarness harness) =>
            harness.Text(@"SELECT ic.relname FROM pg_index ix
                  JOIN pg_class ic ON ic.oid = ix.indexrelid
                  WHERE ix.indrelid = to_regclass(@Table)
                  AND ix.indisunique AND ix.indpred IS NULL
                  AND ix.indnkeyatts = 2
                  AND EXISTS (SELECT 1 FROM pg_attribute a
                              WHERE a.attrelid = ix.indrelid AND a.attnum IN (ix.indkey[0], ix.indkey[1]) AND lower(a.attname) = 'queueid')
                  AND EXISTS (SELECT 1 FROM pg_attribute a
                              WHERE a.attrelid = ix.indrelid AND a.attnum IN (ix.indkey[0], ix.indkey[1]) AND lower(a.attname) = 'exceptiontype')",
                ("@Table", ErrorTrackingTable(harness)));

        private static void InsertErrorRow(PostgreSqlQueueHarness harness, long queueId,
            string exceptionType, int retryCount)
        {
            using var connection = harness.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                $"insert into {ErrorTrackingTable(harness)} (QueueID, ExceptionType, RetryCount) " +
                "values (@QueueID, @ExceptionType, @RetryCount)";
            Add(command, "@QueueID", queueId);
            Add(command, "@ExceptionType", exceptionType);
            Add(command, "@RetryCount", retryCount);
            command.ExecuteNonQuery();
        }

        private static List<(long QueueId, string ExceptionType, int RetryCount)> ReadErrorRows(
            PostgreSqlQueueHarness harness)
        {
            var rows = new List<(long, string, int)>();
            using var connection = harness.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                $"select QueueID, ExceptionType, RetryCount from {ErrorTrackingTable(harness)} " +
                "order by QueueID, ExceptionType";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                rows.Add((Convert.ToInt64(reader.GetValue(0)), reader.GetString(1),
                    Convert.ToInt32(reader.GetValue(2))));
            }

            return rows;
        }

        private static int RetryCountFor(List<(long QueueId, string ExceptionType, int RetryCount)> rows,
            long queueId, string exceptionType)
        {
            foreach (var row in rows)
            {
                if (row.QueueId == queueId && row.ExceptionType == exceptionType)
                    return row.RetryCount;
            }

            Assert.Fail($"no row for ({queueId}, {exceptionType})");
            return 0;
        }

        private static void Add(System.Data.Common.DbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }
    }
}
