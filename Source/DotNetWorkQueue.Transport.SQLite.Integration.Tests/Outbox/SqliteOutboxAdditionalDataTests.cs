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
using System.Data.SQLite;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Outbox
{
    /// <summary>
    /// Integration tests proving <see cref="AdditionalMessageData"/> survives the
    /// caller-transaction Send path on SQLite. Verification uses direct SQL queries against
    /// the queue's MetaData table rather than a live consumer dequeue, avoiding consumer
    /// lifecycle complexity.
    ///
    /// Assertions:
    ///   1. Exactly 1 metadata row after commit (write confirmation).
    ///   2. The CorrelationID persisted in the MetaData table matches the Guid returned by
    ///      <c>sendResult.SentMessage.CorrelationId.Id.Value</c>. Auto-assigned by
    ///      <c>GenerateMessageHeaders.HeaderSetup</c>; the persisted value must equal what
    ///      the producer returned to the caller.
    ///   3. The Priority column persists the caller-supplied value — the regression guard
    ///      catches a silent drop of <c>IAdditionalMessageData</c> on the external-transaction
    ///      path (ISSUE-037).
    /// </summary>
    [TestClass]
    public class SqliteOutboxAdditionalDataTests : SqliteOutboxIntegrationTestBase
    {
        [ClassInitialize]
        public static void Init(TestContext _) => EnsureActivityListenerRegistered();

        private const byte ExpectedPriority = 7;

        /// <summary>
        /// Verifies that CorrelationID and Priority both round-trip through the external-transaction
        /// Send path to the MetaData table. Runs for both in-memory and file-based SQLite to confirm
        /// the outbox contract holds regardless of connection mode.
        /// </summary>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void AdditionalMessageData_RoundTrip_PreservesHeadersAndCorrelation(bool inMemory)
        {
            using var connInfo = new IntegrationConnectionInfo(inMemory);
            var qc = new QueueConnection(NewQueueName(), connInfo.ConnectionString);
            using var queue = CreateQueue(qc, enablePriority: true);
            using var producer = CreateRelationalProducer(qc);

            var data = new AdditionalMessageData();
            // WHY: Set priority explicitly. Unlike CorrelationID (auto-assigned by
            //      GenerateMessageHeaders.HeaderSetup), Priority has no fallback in the
            //      producer path — asserting its persisted value catches a regression where
            //      the producer silently drops AdditionalMessageData on the external-transaction
            //      path (ISSUE-037).
            data.SetPriority(ExpectedPriority);
            var msg = GenerateMessage.Create<FakeMessage>();

            IQueueOutputMessage sendResult;
            using var conn = new SQLiteConnection(connInfo.ConnectionString);
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                sendResult = producer.RelationalProducer.Send(msg, data, transaction);
                Assert.IsFalse(sendResult.HasError, sendResult.SendingException?.ToString());
                transaction.Commit();
            }

            // Sanity: exactly one metadata row was committed.
            AssertQueueRowCount(qc, 1);

            // Retrieve the CorrelationID that the producer returned to the caller.
            var returnedCorrelationGuid = (Guid)sendResult.SentMessage.CorrelationId.Id.Value;

            // Query the MetaData table directly and assert the persisted GUID matches.
            AssertCorrelationIdInMetadata(qc, returnedCorrelationGuid);

            // Priority round-trip: the regression guard described above.
            AssertPriorityInMetadata(qc, ExpectedPriority);
        }

        /// <summary>
        /// Queries the queue's MetaData table for a single CorrelationID row and asserts it
        /// equals <paramref name="expected"/>. Opens a fresh connection to isolate the read
        /// from the producer's commit visibility.
        /// </summary>
        private static void AssertCorrelationIdInMetadata(QueueConnection queueConnection, Guid expected)
        {
            var info = new SqliteConnectionInformation(queueConnection, new DbDataSource());
            var helper = new TableNameHelper(info);
            // WHY: Fresh connection per assertion — isolates the read from the producer
            //      connection and absorbs any SQLite commit-visibility edge cases.
            using var conn = new SQLiteConnection(queueConnection.Connection);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT CorrelationID FROM {helper.MetaDataName} LIMIT 1";
            var raw = cmd.ExecuteScalar();
            // WHY: SQLite stores CorrelationID via DbType.StringFixedLength size 38
            //      (SendMessage.cs lines 124, 182). Read-back is a string; Guid.Parse
            //      tolerates both {B} and D formats produced by the SQLite driver.
            var actual = Guid.Parse((string)raw!);
            Assert.AreEqual(expected, actual,
                $"CorrelationID did not round-trip: expected {expected}, persisted {actual}.");
        }

        /// <summary>
        /// Queries the queue's MetaData table for the Priority column and asserts the single
        /// persisted row equals <paramref name="expected"/>. Opens a fresh connection to isolate
        /// the read from the producer's commit visibility.
        /// </summary>
        private static void AssertPriorityInMetadata(QueueConnection queueConnection, byte expected)
        {
            var info = new SqliteConnectionInformation(queueConnection, new DbDataSource());
            var helper = new TableNameHelper(info);
            // WHY: Fresh connection per assertion — same isolation rationale as AssertCorrelationIdInMetadata.
            using var conn = new SQLiteConnection(queueConnection.Connection);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT Priority FROM {helper.MetaDataName} LIMIT 1";
            var raw = cmd.ExecuteScalar();
            // WHY: SQLite INTEGER affinity surfaces from ExecuteScalar as long;
            //      Convert.ToByte handles the narrowing safely (same idiom as PG test).
            var actual = Convert.ToByte(raw);
            Assert.AreEqual(expected, actual,
                $"Priority did not round-trip: expected {expected}, persisted {actual}.");
        }
    }
}
