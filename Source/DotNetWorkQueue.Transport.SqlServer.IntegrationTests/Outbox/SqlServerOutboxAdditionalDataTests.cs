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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SqlServer;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Outbox
{
    /// <summary>
    /// Integration test proving <see cref="AdditionalMessageData"/> survives the caller-transaction
    /// Send path. Verification uses a direct SQL query against the queue's MetaData table
    /// (same pattern as <see cref="DotNetWorkQueue.Transport.SqlServer.IntegrationTests.VerifyQueueData.VerifyPriority"/>)
    /// rather than a live consumer dequeue, avoiding consumer lifecycle complexity.
    ///
    /// Assertions:
    ///   1. Exactly 1 metadata row after commit (sanity / write confirmation).
    ///   2. The CorrelationID persisted in the MetaData table matches the Guid returned
    ///      by <c>sendResult.SentMessage.CorrelationId.Id.Value</c>. The correlation ID is
    ///      auto-assigned by <c>GenerateMessageHeaders.HeaderSetup</c> when not pre-set,
    ///      and the persisted value must equal what the producer returned to the caller.
    ///   3. The Priority column persists the caller-supplied value. Priority has NO
    ///      auto-assignment fallback in the producer, so this assertion specifically
    ///      catches a regression where the producer silently drops the caller-supplied
    ///      <c>IAdditionalMessageData</c> on the external-transaction path.
    ///   4. The send result contains no error (confirming the caller-transaction path succeeded).
    /// </summary>
    [TestClass]
    public class SqlServerOutboxAdditionalDataTests : SqlServerOutboxIntegrationTestBase
    {
        private const byte ExpectedPriority = 7;

        [TestMethod]
        public void AdditionalMessageData_RoundTrip_PreservesHeadersAndCorrelation()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            using var queue = CreateQueue(qc, enablePriority: true);
            using var producer = CreateRelationalProducer(qc);

            var data = new AdditionalMessageData();
            // Set priority explicitly. Unlike CorrelationID (auto-assigned by
            // GenerateMessageHeaders.HeaderSetup), Priority has no fallback in the
            // producer path — so asserting on its persisted value catches a regression
            // where the producer drops the caller-supplied AdditionalMessageData on the
            // external-transaction path. (ISSUE-037)
            data.SetPriority(ExpectedPriority);
            var msg = GenerateMessage.Create<FakeMessage>();

            IQueueOutputMessage sendResult;
            using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
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
        /// Queries the queue's MetaData table for a single CorrelationID row and asserts
        /// it equals <paramref name="expected"/>. Mirrors the table-access pattern in
        /// <c>VerifyQueueData.VerifyPriority()</c>.
        /// </summary>
        private static void AssertCorrelationIdInMetadata(QueueConnection qc, Guid expected)
        {
            var info = new SqlConnectionInformation(qc);
            var helper = new SqlServerTableNameHelper(info);
            using var conn = new SqlConnection(qc.Connection);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT CorrelationID FROM {helper.MetaDataName}";
            using var reader = cmd.ExecuteReader();
            Assert.IsTrue(reader.Read(), "Expected at least one row in MetaData table.");
            var actual = (Guid)reader[0];
            Assert.AreEqual(expected, actual,
                $"CorrelationID did not round-trip: expected {expected}, persisted {actual}.");
        }

        /// <summary>
        /// Queries the queue's MetaData table for the Priority column and asserts the
        /// single persisted row equals <paramref name="expected"/>. Mirrors the
        /// pattern in <c>VerifyQueueData.VerifyPriority()</c>.
        /// </summary>
        private static void AssertPriorityInMetadata(QueueConnection qc, byte expected)
        {
            var info = new SqlConnectionInformation(qc);
            var helper = new SqlServerTableNameHelper(info);
            using var conn = new SqlConnection(qc.Connection);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT priority FROM {helper.MetaDataName}";
            using var reader = cmd.ExecuteReader();
            Assert.IsTrue(reader.Read(), "Expected at least one row in MetaData table.");
            var actual = (byte)reader[0];
            Assert.AreEqual(expected, actual,
                $"Priority did not round-trip: expected {expected}, persisted {actual}.");
        }
    }
}
