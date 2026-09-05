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
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Outbox
{
    /// <summary>
    /// A caller-supplied transaction still writes the status row, and still writes it inside the
    /// caller's transaction.
    /// </summary>
    /// <remarks>
    /// An ordinary send now folds the status insert into its single statement, so
    /// <c>CreateStatusRecord</c> is reached only by the two paths that keep a transaction open -
    /// a scheduled job and a caller-supplied transaction. The outbox suite ran with the status
    /// table off, which left the second of those with no coverage at all: the queue could be
    /// created with a status table and the outbox producer would simply never populate it, or
    /// populate it outside the caller's transaction, and nothing would say so.
    /// </remarks>
    [TestClass]
    public class PostgreSqlOutboxStatusTableTests : PostgreSqlOutboxIntegrationTestBase
    {
        [TestMethod]
        public void Send_StatusTable_Commit_WritesStatusRow()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc, enableStatusTable: true);
            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                using (var transaction = conn.BeginTransaction())
                {
                    var msg = GenerateMessage.Create<FakeMessage>();
                    var result = producer.RelationalProducer.Send(msg, transaction);
                    Assert.IsFalse(result.HasError, result.SendingException?.ToString());
                    InsertBusinessRow(conn, transaction, businessTable, 1, "first");
                    transaction.Commit();
                }

                AssertQueueRowCount(qc, 1);
                AssertBusinessRowExists(conn, businessTable, 1);
                Assert.AreEqual(1, CountStatusRows(qc),
                    "the status row is written by a separate command on the caller's connection, "
                    + "so it has to be sent as well as the meta row");
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }

        [TestMethod]
        public void Send_StatusTable_Rollback_WritesNoStatusRow()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc, enableStatusTable: true);
            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                using (var transaction = conn.BeginTransaction())
                {
                    var msg = GenerateMessage.Create<FakeMessage>();
                    var result = producer.RelationalProducer.Send(msg, transaction);
                    Assert.IsFalse(result.HasError, result.SendingException?.ToString());
                    InsertBusinessRow(conn, transaction, businessTable, 1, "first");
                    transaction.Rollback();
                }

                AssertQueueRowCount(qc, 0);
                AssertBusinessRowExists(conn, businessTable, 0);
                Assert.AreEqual(0, CountStatusRows(qc),
                    "the status row must be enrolled in the caller's transaction like the others - "
                    + "a row left behind here is a message the queue thinks exists");
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }

        /// <summary>
        /// The asynchronous handler carries its own copy of the caller-supplied-transaction path,
        /// including its own status write, so it needs its own coverage.
        /// </summary>
        [TestMethod]
        public async Task SendAsync_StatusTable_Commit_WritesStatusRow()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc, enableStatusTable: true);
            await using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            await conn.OpenAsync();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                await using (var transaction = await conn.BeginTransactionAsync())
                {
                    var msg = GenerateMessage.Create<FakeMessage>();
                    var result = await producer.RelationalProducer.SendAsync(msg, transaction);
                    Assert.IsFalse(result.HasError, result.SendingException?.ToString());
                    InsertBusinessRow(conn, (NpgsqlTransaction)transaction, businessTable, 1, "first");
                    await transaction.CommitAsync();
                }

                AssertQueueRowCount(qc, 1);
                AssertBusinessRowExists(conn, businessTable, 1);
                Assert.AreEqual(1, CountStatusRows(qc));
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }

        private static long CountStatusRows(QueueConnection queueConnection)
        {
            var helper = new TableNameHelper(new SqlConnectionInformation(queueConnection));
            using var conn = new NpgsqlConnection(queueConnection.Connection);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {helper.StatusName}";
            return (long)cmd.ExecuteScalar();
        }
    }
}
