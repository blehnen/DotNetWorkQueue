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
using System.Collections.Generic;
using System.Data.SQLite;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Outbox
{
    /// <summary>
    /// Base class for SQLite outbox integration tests. Mirrors the shape of
    /// <c>PostgreSqlOutboxIntegrationTestBase</c> with SQLite-specific adaptations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>BEGIN EXCLUSIVE-on-write serialization:</b> SQLite serializes all writers via
    /// <c>BEGIN EXCLUSIVE</c> semantics when writing. The caller's <see cref="SQLiteConnection"/>
    /// and <see cref="SQLiteTransaction"/> is the exclusive writer for the duration of any
    /// <c>Send</c> call. Do not introduce concurrent producer scenarios in derived classes;
    /// SQLite's lock semantics differ fundamentally from PostgreSQL and SQL Server, and
    /// concurrent scenarios would fail in shape-specific ways unrelated to the outbox contract
    /// under test. All outbox tests derived from this base are serial-by-design.
    /// </para>
    /// <para>
    /// <b>WSL builders — SQLite.Interop.dll lock workaround:</b> if the native interop DLL
    /// is locked during repeated test runs (common on WSL2 with Windows file system mounts),
    /// redirect build output via:
    /// <code>dotnet build -o /tmp/sqlite_it_build "Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj"</code>
    /// (CLAUDE.md Phase 2 lesson).
    /// </para>
    /// </remarks>
    public abstract class SqliteOutboxIntegrationTestBase
    {
        /// <summary>
        /// Generates a fresh queue name that satisfies DNQ's alphanumeric/underscore/dot constraint
        /// (no hyphens — <see cref="Guid.ToString(string)"/> with "N" strips them).
        /// </summary>
        protected static string NewQueueName() => "q" + Guid.NewGuid().ToString("N");

        /// <summary>
        /// Generates a fresh business-table name. The 32-char hex suffix keeps the total
        /// length well under SQLite's identifier limit.
        /// </summary>
        protected static string NewBusinessTableName() => "outboxbusiness_" + Guid.NewGuid().ToString("N");

        // ---- Queue lifecycle ----

        /// <summary>
        /// Holds queue creation resources and disposes them in safe order on test teardown.
        /// </summary>
        protected sealed class QueueScope : IDisposable
        {
            public QueueCreationContainer<SqLiteMessageQueueInit> QueueCreator { get; init; }
            public SqLiteMessageQueueCreation OCreation { get; init; }
            public ICreationScope Scope { get; init; }

            public void Dispose()
            {
                try { OCreation?.RemoveQueue(); } catch { /* swallow — best-effort cleanup */ }
                OCreation?.Dispose();
                Scope?.Dispose();
                QueueCreator?.Dispose();
            }
        }

        /// <summary>
        /// Creates the queue tables in the SQLite database identified by
        /// <paramref name="queueConnection"/> and returns a <see cref="QueueScope"/>
        /// that removes the queue on disposal.
        /// </summary>
        /// <remarks>
        /// <c>EnableHoldTransactionUntilMessageCommitted</c> is always <c>false</c> — holding
        /// the dequeue transaction on SQLite would deadlock because SQLite's exclusive write lock
        /// blocks the worker's own next dequeue attempt (CLAUDE.md SQLite inbox/outbox lesson).
        /// </remarks>
        protected QueueScope CreateQueue(QueueConnection queueConnection, bool enablePriority = false)
        {
            var queueCreator = new QueueCreationContainer<SqLiteMessageQueueInit>();
            var oCreation = queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection);
            oCreation.Options.EnableStatus = true;
            oCreation.Options.EnableStatusTable = false;
            oCreation.Options.EnableHeartBeat = false;
            oCreation.Options.EnableDelayedProcessing = false;
            oCreation.Options.EnableMessageExpiration = false;
            // EnableHoldTransactionUntilMessageCommitted is not set here — it is an explicit
            // ITransportOptions implementation on SqLiteMessageQueueTransportOptions, hardwired
            // to false with a discarded setter (SQLite BEGIN EXCLUSIVE semantics make hold-tx
            // structurally non-viable; see issue #149 and the class XML doc above).
            oCreation.Options.EnablePriority = enablePriority;

            var result = oCreation.CreateQueue();
            Assert.IsTrue(result.Success, result.ErrorMessage);

            return new QueueScope
            {
                QueueCreator = queueCreator,
                OCreation = oCreation,
                Scope = oCreation.Scope
            };
        }

        // ---- Producer lifecycle ----

        /// <summary>
        /// Holds producer resources and disposes them in safe order on test teardown.
        /// </summary>
        protected sealed class ProducerScope : IDisposable
        {
            public QueueContainer<SqLiteMessageQueueInit> Creator { get; init; }
            public IProducerQueue<FakeMessage> Producer { get; init; }
            public IRelationalProducerQueue<FakeMessage> RelationalProducer { get; init; }

            public void Dispose()
            {
                Producer?.Dispose();
                Creator?.Dispose();
            }
        }

        /// <summary>
        /// Creates a producer for <paramref name="queueConnection"/> and asserts it implements
        /// <see cref="IRelationalProducerQueue{T}"/> — that interface is the outbox contract
        /// (PROJECT.md success criterion #5).
        /// </summary>
        protected ProducerScope CreateRelationalProducer(QueueConnection queueConnection)
        {
            var creator = new QueueContainer<SqLiteMessageQueueInit>();
            var producer = creator.CreateProducer<FakeMessage>(queueConnection);
            Assert.IsInstanceOfType(producer, typeof(IRelationalProducerQueue<FakeMessage>),
                "SQLite producer must implement IRelationalProducerQueue<T> (PROJECT.md success criterion #5)");
            var rp = (IRelationalProducerQueue<FakeMessage>)producer;
            return new ProducerScope { Creator = creator, Producer = producer, RelationalProducer = rp };
        }

        // ---- Business table lifecycle (SQLite) ----

        /// <summary>
        /// Creates a minimal business table in the same SQLite database as the queue,
        /// used to exercise outbox atomicity.
        /// </summary>
        protected static void CreateBusinessTable(SQLiteConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE TABLE {tableName} (Id INTEGER NOT NULL, Val TEXT NOT NULL)";
            cmd.ExecuteNonQuery();
        }

        protected static void DropBusinessTable(SQLiteConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"DROP TABLE IF EXISTS {tableName}";
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Inserts a single row into the business table under the same
        /// <paramref name="transaction"/> as the queue send, so both succeed or roll back
        /// together.
        /// </summary>
        protected static void InsertBusinessRow(SQLiteConnection conn, SQLiteTransaction transaction, string tableName, int id, string val)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = $"INSERT INTO {tableName} (Id, Val) VALUES (@id, @val)";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@val", val);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Returns the row count of the business table. SQLite's INTEGER affinity surfaces
        /// as <c>long</c> from <c>ExecuteScalar</c>, so <see cref="Convert.ToInt64"/> is used
        /// to handle the object cast safely.
        /// </summary>
        protected static long CountBusinessRows(SQLiteConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {tableName}";
            return Convert.ToInt64(cmd.ExecuteScalar());
        }

        // ---- Queue row counting ----

        /// <summary>
        /// Opens a raw <see cref="SQLiteConnection"/> and counts rows in the queue's
        /// MetaData table. Uses <see cref="SqliteConnectionInformation"/> +
        /// <see cref="TableNameHelper"/> to resolve the table name — same pattern as
        /// <c>SharedClasses.Helpers.Verify</c> — so this helper does not couple to the
        /// production DAL.
        /// </summary>
        protected static long CountQueueMessages(QueueConnection queueConnection)
        {
            var info = new SqliteConnectionInformation(queueConnection, new DbDataSource());
            var helper = new TableNameHelper(info);
            using var conn = new SQLiteConnection(queueConnection.Connection);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {helper.MetaDataName}";
            return Convert.ToInt64(cmd.ExecuteScalar());
        }

        /// <summary>
        /// Polls <see cref="CountQueueMessages"/> until the count converges to
        /// <paramref name="expected"/> or <paramref name="timeoutMs"/> elapses. The polling
        /// pattern absorbs SQLite commit-visibility races (CLAUDE.md metrics-race lesson).
        /// </summary>
        protected static void AssertQueueRowCount(QueueConnection queueConnection, long expected, int timeoutMs = 5000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            long actual = -1;
            while (DateTime.UtcNow < deadline)
            {
                actual = CountQueueMessages(queueConnection);
                if (actual == expected) return;
                System.Threading.Thread.Sleep(100);
            }
            Assert.AreEqual(expected, actual,
                $"Queue row count did not converge to {expected} within {timeoutMs}ms (last observed: {actual}).");
        }

        protected static void AssertBusinessRowExists(SQLiteConnection conn, string tableName, long expectedCount)
        {
            var actual = CountBusinessRows(conn, tableName);
            Assert.AreEqual(expectedCount, actual,
                $"Business table {tableName} expected {expectedCount} rows, observed {actual}.");
        }

        // ---- Batch helpers ----

        /// <summary>
        /// Builds a list of <paramref name="count"/> queue messages using
        /// <see cref="GenerateMessage.Create{T}"/> so AutoFixture-generated data exercises
        /// serialization paths.
        /// </summary>
        protected static List<QueueMessage<FakeMessage, IAdditionalMessageData>> BuildBatch(int count)
        {
            var list = new List<QueueMessage<FakeMessage, IAdditionalMessageData>>(count);
            for (var i = 0; i < count; i++)
            {
                list.Add(new QueueMessage<FakeMessage, IAdditionalMessageData>(
                    GenerateMessage.Create<FakeMessage>(), null));
            }
            return list;
        }
    }
}
