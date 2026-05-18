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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Outbox
{
    /// <summary>
    /// Integration test proving <see cref="AdditionalMessageData"/> survives the
    /// caller-transaction Send path on SQLite. Verification queries the queue's
    /// MetaData table directly (mirrors VerifyQueueData.VerifyPriority's pattern)
    /// rather than running a consumer dequeue.
    ///
    /// SQLite-specific column shapes:
    ///   - CorrelationID: TEXT (38) — stored as the GUID's hyphenated string form.
    ///   - Priority: INTEGER — read via Convert.ToInt32 to match VerifyQueueData.
    /// </summary>
    [TestClass]
    public class SqLiteOutboxAdditionalDataTests : SqLiteOutboxIntegrationTestBase
    {
        private const byte ExpectedPriority = 7;

        [ClassInitialize]
        public static void Init(TestContext _) => EnsureActivityListenerRegistered();

        [TestMethod]
        public void AdditionalMessageData_RoundTrip_PreservesHeadersAndCorrelation()
        {
            using var dbScope = new IntegrationConnectionInfo(inMemory: false);
            var qc = new QueueConnection(NewQueueName(), dbScope.ConnectionString);

            using var queue = CreateQueue(qc, enablePriority: true);
            using var producer = CreateRelationalProducer(qc);

            var data = new AdditionalMessageData();
            data.SetPriority(ExpectedPriority);
            var msg = GenerateMessage.Create<FakeMessage>();

            IQueueOutputMessage sendResult;
            using var conn = new SQLiteConnection(dbScope.ConnectionString);
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                sendResult = producer.RelationalProducer.Send(msg, data, transaction);
                Assert.IsFalse(sendResult.HasError, sendResult.SendingException?.ToString());
                AssertCallerResourcesUnmutated(conn, transaction);
                transaction.Commit();
            }

            AssertQueueRowCount(qc, 1);

            var returnedCorrelationGuid = (Guid)sendResult.SentMessage.CorrelationId.Id.Value;
            AssertCorrelationIdInMetadata(qc, returnedCorrelationGuid);
            AssertPriorityInMetadata(qc, ExpectedPriority);
        }

        private static void AssertCorrelationIdInMetadata(QueueConnection qc, Guid expected)
        {
            using var conn = new SQLiteConnection(qc.Connection);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT CorrelationID FROM {qc.Queue}MetaData";
            using var reader = cmd.ExecuteReader();
            Assert.IsTrue(reader.Read(), "Expected at least one row in MetaData table.");
            var raw = reader.GetValue(0)?.ToString();
            Assert.IsTrue(Guid.TryParse(raw, out var actual),
                $"CorrelationID column did not contain a parseable GUID: {raw}");
            Assert.AreEqual(expected, actual,
                $"CorrelationID did not round-trip: expected {expected}, persisted {actual}.");
        }

        private static void AssertPriorityInMetadata(QueueConnection qc, byte expected)
        {
            using var conn = new SQLiteConnection(qc.Connection);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT Priority FROM {qc.Queue}MetaData";
            using var reader = cmd.ExecuteReader();
            Assert.IsTrue(reader.Read(), "Expected at least one row in MetaData table.");
            var actual = Convert.ToByte(reader[0]);
            Assert.AreEqual(expected, actual,
                $"Priority did not round-trip: expected {expected}, persisted {actual}.");
        }
    }
}
