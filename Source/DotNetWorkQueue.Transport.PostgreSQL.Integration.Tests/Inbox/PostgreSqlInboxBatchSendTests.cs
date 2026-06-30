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
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Inbox
{
    /// <summary>
    /// Integration tests for the PostgreSQL inbox held-transaction batch send (#167): a true
    /// multi-row insert performed inside the caller's transaction. Proves commit/rollback
    /// visibility, that the caller's connection is left open and usable, that a real DB failure
    /// throws (rather than being swallowed into per-message results), one result id per message,
    /// and that the unnest/RETURNING + ascending-sort id recovery preserves caller input order
    /// across a multi-chunk batch inside a single external transaction.
    /// </summary>
    [TestClass]
    public class PostgreSqlInboxBatchSendTests : PostgreSqlInboxIntegrationTestBase
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
                using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
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
                using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    var result = producer.RelationalProducer.Send(BuildBatch(batchSize), transaction);
                    Assert.IsFalse(result.HasErrors, "batch send reported errors");
                    for (var i = 0; i < batchSize; i++)
                        InsertBusinessRowOnInboxTransaction(transaction, businessTable, i, $"row{i}");
                    transaction.Rollback();
                }

                // Rollback is synchronous, so the first poll already observes 0 (no lag to wait out).
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
            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();

            using (var transaction = conn.BeginTransaction())
            {
                var result = producer.RelationalProducer.Send(BuildBatch(batchSize), transaction);
                Assert.IsFalse(result.HasErrors, "batch send reported errors");

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

            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();

            var threw = false;
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // The multi-row body insert succeeds; the per-message meta insert then hits the
                    // dropped MetaData table and the batch handler throws. The held-transaction path
                    // performs no table-existence pre-check, so the throw originates at the insert and
                    // propagates out of Send (no swallow into per-message results).
                    producer.RelationalProducer.Send(BuildBatch(3), transaction);
                }
                catch
                {
                    threw = true;
                }
                finally
                {
                    // Roll back defensively; never let a rollback error mask the assertion below.
                    try { transaction.Rollback(); } catch { /* already in the error path */ }
                }
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
            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
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

        [TestMethod]
        public void SendBatch_MultiChunk_IdsInInputOrder()
        {
            // Validates the unnest/RETURNING + ascending-sort id recovery across a MULTI-CHUNK batch
            // inside one external transaction (the #162 → #167 ordering concern). BatchSize=2 with 5
            // messages forces 3 chunks; recovered ids must be strictly increasing in caller input order
            // (each chunk's bigserial ids are assigned in ORDER BY ord, and later chunks get higher ids).
            const int batchSize = 5;
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);

            using var queue = CreateQueueWithBatchSize(qc, batchSize: 2);
            using var producer = CreateRelationalProducer(qc);
            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();

            using (var transaction = conn.BeginTransaction())
            {
                var result = producer.RelationalProducer.Send(BuildBatch(batchSize), transaction);
                Assert.IsFalse(result.HasErrors, "batch send reported errors");
                Assert.AreEqual(batchSize, result.Count, "one result per message");

                long previous = 0;
                for (var i = 0; i < result.Count; i++)
                {
                    var id = (long)result[i].SentMessage.MessageId.Id.Value;
                    Assert.IsTrue(id > previous,
                        $"recovered ids must be strictly increasing in input order across chunks (index {i}: {id} <= {previous})");
                    previous = id;
                }

                transaction.Commit();
            }

            AssertQueueRowCount(qc, batchSize);
        }

        // ----- Producer resolution + capability cast (mirrors the outbox base) -----
        private sealed class ProducerScope : System.IDisposable
        {
            private int _disposed;

            public QueueContainer<PostgreSqlMessageQueueInit> Creator { get; init; }
            public IProducerQueue<FakeMessage> Producer { get; init; }
            public IRelationalProducerQueue<FakeMessage> RelationalProducer { get; init; }

            public void Dispose()
            {
                if (System.Threading.Interlocked.Exchange(ref _disposed, 1) != 0) return;
                Producer?.Dispose();
                Creator?.Dispose();
            }
        }

        private static ProducerScope CreateRelationalProducer(QueueConnection queueConnection)
        {
            var creator = new QueueContainer<PostgreSqlMessageQueueInit>();
            var producer = creator.CreateProducer<FakeMessage>(queueConnection);
            Assert.IsInstanceOfType(producer, typeof(IRelationalProducerQueue<FakeMessage>),
                "PostgreSQL producer must implement IRelationalProducerQueue<T> (#167 inbox batch path)");
            return new ProducerScope
            {
                Creator = creator,
                Producer = producer,
                RelationalProducer = (IRelationalProducerQueue<FakeMessage>)producer
            };
        }

        // ----- Queue creation with an explicit BatchSize (for the multi-chunk ordering test) -----
        private QueueScope CreateQueueWithBatchSize(QueueConnection queueConnection, int batchSize)
        {
            var queueCreator = new QueueCreationContainer<PostgreSqlMessageQueueInit>();
            var oCreation = queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
            oCreation.Options.EnableStatus = false;
            oCreation.Options.EnableStatusTable = false;
            oCreation.Options.EnableHeartBeat = false;
            oCreation.Options.EnableDelayedProcessing = false;
            oCreation.Options.EnableMessageExpiration = false;
            oCreation.Options.EnableHoldTransactionUntilMessageCommitted = true;
            oCreation.Options.EnablePriority = false;
            oCreation.Options.BatchSize = batchSize;

            var result = oCreation.CreateQueue();
            Assert.IsTrue(result.Success, result.ErrorMessage);

            return new QueueScope
            {
                QueueCreator = queueCreator,
                OCreation = oCreation,
                Scope = oCreation.Scope
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
        private static long CountQueueMessages(QueueConnection queueConnection)
        {
            var info = new SqlConnectionInformation(queueConnection);
            var helper = new TableNameHelper(info);
            using var conn = new NpgsqlConnection(queueConnection.Connection);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {helper.MetaDataName}";
            return (long)cmd.ExecuteScalar();
        }

        private static void AssertQueueRowCount(QueueConnection queueConnection, long expected, int timeoutMs = 5000)
        {
            var deadline = System.DateTime.UtcNow.AddMilliseconds(timeoutMs);
            long actual = -1;
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
            var helper = new TableNameHelper(info);
            using var conn = new NpgsqlConnection(queueConnection.Connection);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"DROP TABLE IF EXISTS {helper.MetaDataName}";
            cmd.ExecuteNonQuery();
        }
    }
}
