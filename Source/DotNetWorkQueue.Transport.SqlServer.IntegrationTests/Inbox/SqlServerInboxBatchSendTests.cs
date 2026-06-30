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
using System.Collections.Generic;
using System.Data;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Inbox
{
    /// <summary>
    /// Integration tests for the SqlServer inbox held-transaction batch send (#167): a true
    /// multi-row insert performed inside the caller's transaction. Proves commit/rollback
    /// visibility, that the caller's connection is left open and usable, that a real DB failure
    /// throws (rather than being swallowed into per-message results), and that one result id is
    /// returned per message.
    /// </summary>
    [TestClass]
    public class SqlServerInboxBatchSendTests : SqlServerInboxIntegrationTestBase
    {
        [TestMethod]
        public void SendBatch_Commit_AllRowsVisible()
        {
            const int batchSize = 5;
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc, enableHoldTransaction: true);
            CreateBusinessTable(ConnectionInfo.ConnectionString, businessTable);
            try
            {
                using var producer = CreateRelationalProducer(qc);
                using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    var result = producer.RelationalProducer.Send(BuildBatch(batchSize), transaction);
                    Assert.IsFalse(result.HasErrors, "batch send reported errors");
                    for (var i = 0; i < batchSize; i++)
                        InsertBusinessRowOnInboxTransaction(transaction, businessTable, i, $"row{i}");
                    transaction.Commit();
                }

                AssertQueueRowCount(qc, batchSize);
                AssertBusinessRowCountFromSeparateConnection(ConnectionInfo.ConnectionString, businessTable, batchSize);
            }
            finally
            {
                DropBusinessTable(ConnectionInfo.ConnectionString, businessTable);
            }
        }

        [TestMethod]
        public void SendBatch_Rollback_NoRowsVisible()
        {
            const int batchSize = 5;
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc, enableHoldTransaction: true);
            CreateBusinessTable(ConnectionInfo.ConnectionString, businessTable);
            try
            {
                using var producer = CreateRelationalProducer(qc);
                using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    var result = producer.RelationalProducer.Send(BuildBatch(batchSize), transaction);
                    Assert.IsFalse(result.HasErrors, "batch send reported errors");
                    for (var i = 0; i < batchSize; i++)
                        InsertBusinessRowOnInboxTransaction(transaction, businessTable, i, $"row{i}");
                    transaction.Rollback();
                }

                // Nothing was committed: neither the queue rows nor the business rows are visible.
                AssertQueueRowCount(qc, 0);
                AssertBusinessRowCountStaysAt(ConnectionInfo.ConnectionString, businessTable, 0);
            }
            finally
            {
                DropBusinessTable(ConnectionInfo.ConnectionString, businessTable);
            }
        }

        [TestMethod]
        public void SendBatch_ConnectionStillOpenAndUsable()
        {
            const int batchSize = 3;
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);

            using var queue = CreateQueue(qc, enableHoldTransaction: true);
            using var producer = CreateRelationalProducer(qc);
            using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();

            using (var transaction = conn.BeginTransaction())
            {
                var result = producer.RelationalProducer.Send(BuildBatch(batchSize), transaction);
                Assert.IsFalse(result.HasErrors, "batch send reported errors");

                // The batch path must not close/dispose the caller's connection or transaction.
                Assert.AreEqual(ConnectionState.Open, conn.State, "caller connection must remain open");
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = "SELECT 1";
                    Assert.AreEqual(1, (int)cmd.ExecuteScalar(), "connection must still be usable after send");
                }

                transaction.Commit();
            }

            AssertQueueRowCount(qc, batchSize);
        }

        [TestMethod]
        public void SendBatch_ForcedFailure_Throws()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);

            using var queue = CreateQueue(qc, enableHoldTransaction: true);
            using var producer = CreateRelationalProducer(qc);

            // Drop the MetaData table so the per-message meta insert inside the batch path fails with
            // a real SQL error. The held-transaction path must propagate it (throw) rather than
            // swallow it into per-message results — the caller owns the rollback.
            DropMetaDataTable(qc);

            using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();

            var threw = false;
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    producer.RelationalProducer.Send(BuildBatch(3), transaction);
                }
                catch
                {
                    threw = true;
                }
                transaction.Rollback();
            }

            Assert.IsTrue(threw, "Held-transaction batch send must throw on a DB failure, not swallow it.");
        }

        [TestMethod]
        public void SendBatch_ResultIds_OnePerMessage()
        {
            const int batchSize = 4;
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);

            using var queue = CreateQueue(qc, enableHoldTransaction: true);
            using var producer = CreateRelationalProducer(qc);
            using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();

            using (var transaction = conn.BeginTransaction())
            {
                var result = producer.RelationalProducer.Send(BuildBatch(batchSize), transaction);
                Assert.IsFalse(result.HasErrors, "batch send reported errors");
                Assert.AreEqual(batchSize, result.Count, "one result per message");

                var ids = new HashSet<long>();
                for (var i = 0; i < result.Count; i++)
                {
                    Assert.IsTrue(result[i].SentMessage.MessageId.HasValue, "each result must carry a message id");
                    var id = (long)result[i].SentMessage.MessageId.Id.Value;
                    Assert.IsTrue(id > 0, "message id must be a positive value");
                    ids.Add(id);
                }
                Assert.AreEqual(batchSize, ids.Count, "message ids must be distinct");

                transaction.Commit();
            }

            AssertQueueRowCount(qc, batchSize);
        }

        // ----- Producer resolution + capability cast (mirrors the outbox base) -----
        private sealed class ProducerScope : System.IDisposable
        {
            public QueueContainer<SqlServerMessageQueueInit> Creator { get; init; }
            public IProducerQueue<FakeMessage> Producer { get; init; }
            public IRelationalProducerQueue<FakeMessage> RelationalProducer { get; init; }

            public void Dispose()
            {
                Producer?.Dispose();
                Creator?.Dispose();
            }
        }

        private static ProducerScope CreateRelationalProducer(QueueConnection queueConnection)
        {
            var creator = new QueueContainer<SqlServerMessageQueueInit>();
            var producer = creator.CreateProducer<FakeMessage>(queueConnection);
            Assert.IsInstanceOfType(producer, typeof(IRelationalProducerQueue<FakeMessage>),
                "SqlServer producer must implement IRelationalProducerQueue<T> (#167 inbox batch path)");
            return new ProducerScope
            {
                Creator = creator,
                Producer = producer,
                RelationalProducer = (IRelationalProducerQueue<FakeMessage>)producer
            };
        }

        // ----- Batch builder -----
        private static List<QueueMessage<FakeMessage, IAdditionalMessageData>> BuildBatch(int count)
        {
            var list = new List<QueueMessage<FakeMessage, IAdditionalMessageData>>(count);
            for (var i = 0; i < count; i++)
                list.Add(new QueueMessage<FakeMessage, IAdditionalMessageData>(
                    GenerateMessage.Create<FakeMessage>(), null));
            return list;
        }

        // ----- Queue MetaData row count (polling, NOT snapshot — CLAUDE.md lesson) -----
        private static int CountQueueMessages(QueueConnection queueConnection)
        {
            var info = new SqlConnectionInformation(queueConnection);
            var helper = new SqlServerTableNameHelper(info);
            using var conn = new SqlConnection(queueConnection.Connection);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {helper.MetaDataName}";
            return (int)cmd.ExecuteScalar();
        }

        private static void AssertQueueRowCount(QueueConnection queueConnection, int expected, int timeoutMs = 5000)
        {
            var deadline = System.DateTime.UtcNow.AddMilliseconds(timeoutMs);
            int actual = -1;
            while (System.DateTime.UtcNow < deadline)
            {
                actual = CountQueueMessages(queueConnection);
                if (actual == expected) return;
                System.Threading.Thread.Sleep(100);
            }
            Assert.AreEqual(expected, actual,
                $"Queue row count did not converge to {expected} within {timeoutMs}ms (last observed: {actual}).");
        }

        private static void DropMetaDataTable(QueueConnection queueConnection)
        {
            var info = new SqlConnectionInformation(queueConnection);
            var helper = new SqlServerTableNameHelper(info);
            using var conn = new SqlConnection(queueConnection.Connection);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"IF OBJECT_ID('{helper.MetaDataName}', 'U') IS NOT NULL DROP TABLE {helper.MetaDataName}";
            cmd.ExecuteNonQuery();
        }
    }
}
