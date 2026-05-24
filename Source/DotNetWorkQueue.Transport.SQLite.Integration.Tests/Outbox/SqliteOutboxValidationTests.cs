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
using System.Data.Common;
using System.Data.SQLite;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Outbox
{
    /// <summary>
    /// Integration tests proving that <c>SqLiteExternalTransactionValidator</c> runs BEFORE
    /// any SQL write: every guard path throws <see cref="InvalidOperationException"/> and
    /// leaves the queue's metadata table untouched. Also closes the Phase-2 deferral for the
    /// happy-path single-send round-trip (Method 3).
    /// </summary>
    [TestClass]
    public class SqliteOutboxValidationTests : SqliteOutboxIntegrationTestBase
    {
        [ClassInitialize]
        public static void Init(TestContext _) => EnsureActivityListenerRegistered();

        /// <summary>
        /// Guard: cross-database mismatch — transaction belongs to a DIFFERENT SQLite file/URI
        /// than the queue's database. Must throw before any INSERT lands in the queue table.
        /// </summary>
        /// <remarks>
        /// SQLite has no system database (no equivalent of PG's "postgres" or SS's "master"),
        /// so the mismatch is simulated with a SECOND <see cref="IntegrationConnectionInfo"/>
        /// that generates a different file stem. The validator sees <c>actual != expected</c>
        /// after <see cref="System.IO.Path.GetFileNameWithoutExtension"/> normalization and
        /// throws before any queue INSERT runs.
        /// </remarks>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Validation_CrossDatabaseMismatch_ThrowsBeforeInsert(bool inMemory)
        {
            using var primary = new IntegrationConnectionInfo(inMemory);
            var qc = new QueueConnection(NewQueueName(), primary.ConnectionString);
            using var queue = CreateQueue(qc);
            using var producer = CreateRelationalProducer(qc);

            // Open a connection to a DIFFERENT SQLite database (different stem). The validator's
            // DB-name comparison must fire and throw before any INSERT lands in qc's queue table.
            using var other = new IntegrationConnectionInfo(inMemory);
            using var foreignConn = new SQLiteConnection(other.ConnectionString);
            foreignConn.Open();
            using var foreignTransaction = foreignConn.BeginTransaction();

            var ex = Assert.ThrowsExactly<InvalidOperationException>(
                () => producer.RelationalProducer.Send(new FakeMessage(), new AdditionalMessageData(), foreignTransaction));

            // Exception message must mention the database mismatch — both sides are included.
            Assert.IsTrue(
                ex.Message.IndexOf("database", StringComparison.OrdinalIgnoreCase) >= 0,
                $"Expected exception message to describe a database mismatch: {ex.Message}");

            // No partial write — queue MetaData table count must stay at 0.
            AssertQueueRowCount(qc, 0);

            try { foreignTransaction.Rollback(); } catch { /* transaction may already be invalidated */ }
        }

        /// <summary>
        /// Guard: closed connection — transaction's connection is closed before Send is called.
        /// Must throw before any INSERT lands in the queue table.
        /// </summary>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Validation_ClosedConnection_ThrowsBeforeInsert(bool inMemory)
        {
            using var connInfo = new IntegrationConnectionInfo(inMemory);
            var qc = new QueueConnection(NewQueueName(), connInfo.ConnectionString);
            using var queue = CreateQueue(qc);
            using var producer = CreateRelationalProducer(qc);

            var conn = new SQLiteConnection(connInfo.ConnectionString);
            conn.Open();
            var transaction = conn.BeginTransaction();

            // Close the connection — validator check 3 (State != Open) fires before any INSERT.
            conn.Close();

            Assert.ThrowsExactly<InvalidOperationException>(
                () => producer.RelationalProducer.Send(new FakeMessage(), new AdditionalMessageData(), transaction));

            // No partial write.
            AssertQueueRowCount(qc, 0);

            // Cleanup — both objects may already be disposed/invalidated; swallow.
            try { transaction.Dispose(); } catch { /* ignore */ }
            try { conn.Dispose(); } catch { /* ignore */ }
        }

        /// <summary>
        /// Phase 2 unit tests could not exercise the happy-path Send(msg, realSqliteTransaction)
        /// round-trip because System.Data.SQLite.SQLiteTransaction is sealed and NSubstitute
        /// could not mock it. This integration test closes that gap with a real DB.
        /// </summary>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Validation_GuardPass_HappyPath_SingleSend(bool inMemory)
        {
            using var connInfo = new IntegrationConnectionInfo(inMemory);
            var qc = new QueueConnection(NewQueueName(), connInfo.ConnectionString);
            using var queue = CreateQueue(qc);
            using var producer = CreateRelationalProducer(qc);

            // Real SQLiteConnection + real SQLiteTransaction — all guards must pass,
            // Send must succeed, and exactly one row must be visible after Commit.
            using var conn = new SQLiteConnection(connInfo.ConnectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();

            // Assert no throw AND no send error — happy path.
            var result = producer.RelationalProducer.Send(new FakeMessage(), new AdditionalMessageData(), transaction);
            Assert.IsFalse(result.HasError, result.SendingException?.ToString());

            transaction.Commit();

            // Exactly one message row committed.
            AssertQueueRowCount(qc, 1);
        }

        /// <summary>
        /// Guard: wrong transaction type — a <see cref="DbTransaction"/> that is NOT a
        /// <c>System.Data.SQLite.SQLiteTransaction</c> must never allow a queue INSERT.
        /// </summary>
        /// <remarks>
        /// Validator runs connection-state and DB-name checks BEFORE the
        /// <c>GuardSQLiteTransaction</c> type check. A pure
        /// <c>Substitute.For&lt;DbTransaction&gt;()</c> (no connection wired up) will most
        /// likely fail the null-connection check (validator check #2) and throw
        /// <see cref="InvalidOperationException"/> before the type guard fires. The test only
        /// asserts that <see cref="InvalidOperationException"/> is thrown — whichever guard
        /// fires first, the overall contract (no row inserted on bad transaction) holds.
        /// See RESEARCH §6 file 2 for guard ordering.
        /// </remarks>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Validation_WrongTransactionType_ThrowsInvalidOperation(bool inMemory)
        {
            using var connInfo = new IntegrationConnectionInfo(inMemory);
            var qc = new QueueConnection(NewQueueName(), connInfo.ConnectionString);
            using var queue = CreateQueue(qc);
            using var producer = CreateRelationalProducer(qc);

            // NSubstitute cannot mock sealed types; DbTransaction (abstract) is substitutable.
            // Its Connection property returns null by default — triggers validator check #2.
            var wrongTransaction = Substitute.For<DbTransaction>();

            Assert.ThrowsExactly<InvalidOperationException>(
                () => producer.RelationalProducer.Send(new FakeMessage(), new AdditionalMessageData(), wrongTransaction));

            // No partial write regardless of which guard fired.
            AssertQueueRowCount(qc, 0);
        }
    }
}
