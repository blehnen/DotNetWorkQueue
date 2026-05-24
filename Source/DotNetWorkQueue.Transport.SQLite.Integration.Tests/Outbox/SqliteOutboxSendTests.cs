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
using System.Data.SQLite;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Outbox
{
    [TestClass]
    public class SqliteOutboxSendTests : SqliteOutboxIntegrationTestBase
    {
        [ClassInitialize]
        public static void Init(TestContext _) => EnsureActivityListenerRegistered();

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Send_Commit_BothRowsVisible(bool inMemory)
        {
            using var connInfo = new IntegrationConnectionInfo(inMemory);
            var qc = new QueueConnection(NewQueueName(), connInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            // WHY: SQLiteConnection is opened on the caller side so the business table and the
            // queue send share the same connection and transaction — the outbox atomicity contract.
            using var conn = new SQLiteConnection(connInfo.ConnectionString);
            conn.Open();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                // WHY: SQLiteTransaction is returned directly from BeginTransaction; no cast to
                // DbTransaction is required because SQLiteTransaction inherits DbTransaction.
                using (var transaction = conn.BeginTransaction())
                {
                    var msg = GenerateMessage.Create<FakeMessage>();
                    var result = producer.RelationalProducer.Send(msg, new AdditionalMessageData(), transaction);
                    Assert.IsFalse(result.HasError, result.SendingException?.ToString());
                    InsertBusinessRow(conn, transaction, businessTable, 1, "first");
                    transaction.Commit();
                }

                AssertQueueRowCount(qc, 1);
                AssertBusinessRowExists(conn, businessTable, 1);
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Send_Rollback_NeitherRowVisible(bool inMemory)
        {
            using var connInfo = new IntegrationConnectionInfo(inMemory);
            var qc = new QueueConnection(NewQueueName(), connInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            using var conn = new SQLiteConnection(connInfo.ConnectionString);
            conn.Open();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                using (var transaction = conn.BeginTransaction())
                {
                    var msg = GenerateMessage.Create<FakeMessage>();
                    var result = producer.RelationalProducer.Send(msg, new AdditionalMessageData(), transaction);
                    Assert.IsFalse(result.HasError, result.SendingException?.ToString());
                    InsertBusinessRow(conn, transaction, businessTable, 1, "first");
                    transaction.Rollback();
                }

                AssertQueueRowCount(qc, 0);
                AssertBusinessRowExists(conn, businessTable, 0);
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SendBatch_Commit_AllRowsVisible(bool inMemory)
        {
            const int batchSize = 5;
            using var connInfo = new IntegrationConnectionInfo(inMemory);
            var qc = new QueueConnection(NewQueueName(), connInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            using var conn = new SQLiteConnection(connInfo.ConnectionString);
            conn.Open();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                using (var transaction = conn.BeginTransaction())
                {
                    var batch = BuildBatch(batchSize);
                    var result = producer.RelationalProducer.Send(batch, transaction);
                    Assert.IsFalse(result.HasErrors);
                    for (var i = 0; i < batchSize; i++)
                        InsertBusinessRow(conn, transaction, businessTable, i, $"row{i}");
                    transaction.Commit();
                }

                AssertQueueRowCount(qc, batchSize);
                AssertBusinessRowExists(conn, businessTable, batchSize);
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SendBatch_Rollback_NeitherRowVisible(bool inMemory)
        {
            const int batchSize = 5;
            using var connInfo = new IntegrationConnectionInfo(inMemory);
            var qc = new QueueConnection(NewQueueName(), connInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            using var conn = new SQLiteConnection(connInfo.ConnectionString);
            conn.Open();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                using (var transaction = conn.BeginTransaction())
                {
                    var batch = BuildBatch(batchSize);
                    var result = producer.RelationalProducer.Send(batch, transaction);
                    Assert.IsFalse(result.HasErrors);
                    for (var i = 0; i < batchSize; i++)
                        InsertBusinessRow(conn, transaction, businessTable, i, $"row{i}");
                    transaction.Rollback();
                }

                AssertQueueRowCount(qc, 0);
                AssertBusinessRowExists(conn, businessTable, 0);
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }
    }
}
