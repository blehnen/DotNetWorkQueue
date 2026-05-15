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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Outbox
{
    /// <summary>
    /// Validation integration tests for the PostgreSQL outbox producer's caller-transaction path.
    /// Proves the <c>ExternalTransactionValidator</c> runs BEFORE any SQL write:
    /// cross-database mismatch and closed-connection cases both throw
    /// <see cref="InvalidOperationException"/> and leave the queue's metadata table
    /// untouched. Closes PROJECT.md §SC #6.
    /// </summary>
    [TestClass]
    public class PostgreSqlOutboxValidationTests : PostgreSqlOutboxIntegrationTestBase
    {
        [TestMethod]
        public void Validation_CrossDatabaseMismatch_ThrowsBeforeInsert()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            using var queue = CreateQueue(qc);
            using var producer = CreateRelationalProducer(qc);

            // Open an NpgsqlConnection to "postgres" (system DB, always present on PG) —
            // a different database than the queue's configured catalog. The validator's
            // cross-DB check must throw before any INSERT lands in the queue's metadata table.
            var builder = new NpgsqlConnectionStringBuilder(ConnectionInfo.ConnectionString.Trim());
            builder.Database = "postgres";
            using var wrongDb = new NpgsqlConnection(builder.ConnectionString);
            wrongDb.Open();
            using var wrongTx = wrongDb.BeginTransaction();

            var msg = GenerateMessage.Create<FakeMessage>();

            var ex = Assert.ThrowsExactly<InvalidOperationException>(
                () => producer.RelationalProducer.Send(msg, wrongTx));

            // Validator's exception message must include the wrong DB name (PROJECT.md §Diagnostics).
            Assert.IsTrue(ex.Message.IndexOf("postgres", StringComparison.Ordinal) >= 0,
                $"Expected exception message to mention 'postgres': {ex.Message}");

            // No partial write — queue MetaData table count must be 0.
            AssertQueueRowCount(qc, 0);

            try { wrongTx.Rollback(); } catch { /* ignore — transaction may already be invalidated */ }
        }

        [TestMethod]
        public void Validation_ClosedConnection_ThrowsBeforeInsert()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            using var queue = CreateQueue(qc);
            using var producer = CreateRelationalProducer(qc);

            var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();
            var transaction = conn.BeginTransaction();
            // Close the connection. After this either transaction.Connection is null (validator
            // check 2) or its State != Open (validator check 3); either path must throw
            // InvalidOperationException before any INSERT lands.
            conn.Close();

            var msg = GenerateMessage.Create<FakeMessage>();

            Assert.ThrowsExactly<InvalidOperationException>(
                () => producer.RelationalProducer.Send(msg, transaction));

            // No partial write
            AssertQueueRowCount(qc, 0);

            // Cleanup — both objects may already be disposed/invalidated; swallow.
            try { transaction.Dispose(); } catch { /* ignore */ }
            try { conn.Dispose(); } catch { /* ignore */ }
        }
    }
}
