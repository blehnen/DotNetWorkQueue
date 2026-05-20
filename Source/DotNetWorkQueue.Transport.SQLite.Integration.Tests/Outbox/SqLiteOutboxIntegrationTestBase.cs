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
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Threading;
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
    /// Shared base for SQLite outbox integration tests. Mirrors the SqlServer / PostgreSQL
    /// outbox bases with System.Data.SQLite substituted and per-test file-backed databases
    /// (WAL journaling pre-configured by <see cref="IntegrationConnectionInfo"/>).
    /// </summary>
    public abstract class SqLiteOutboxIntegrationTestBase
    {
        protected static string NewQueueName() => "q" + Guid.NewGuid().ToString("N");

        protected static string NewBusinessTableName() => "OutboxBusiness_" + Guid.NewGuid().ToString("N");

        // ---- ActivityListener (mandatory per CLAUDE.md trace-decorator lesson) ----
        private static readonly object ListenerLock = new();
        private static ActivityListener _listener;

        protected static void EnsureActivityListenerRegistered()
        {
            lock (ListenerLock)
            {
                if (_listener != null) return;
                _listener = new ActivityListener
                {
                    ShouldListenTo = src => src.Name.StartsWith("DotNetWorkQueue", StringComparison.Ordinal),
                    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
                };
                ActivitySource.AddActivityListener(_listener);
            }
        }

        protected sealed class QueueScope : IDisposable
        {
            public QueueCreationContainer<SqLiteMessageQueueInit> QueueCreator { get; init; }
            public SqLiteMessageQueueCreation OCreation { get; init; }
            public ICreationScope Scope { get; init; }

            private int _disposed;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
                try { OCreation?.RemoveQueue(); } catch { /* swallow */ }
                OCreation?.Dispose();
                Scope?.Dispose();
                QueueCreator?.Dispose();
            }
        }

        protected QueueScope CreateQueue(QueueConnection queueConnection, bool enablePriority = false)
        {
            var queueCreator = new QueueCreationContainer<SqLiteMessageQueueInit>();
            var oCreation = queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection);
            oCreation.Options.EnableStatus = true;
            oCreation.Options.EnableStatusTable = false;
            oCreation.Options.EnableHeartBeat = false;
            oCreation.Options.EnableDelayedProcessing = false;
            oCreation.Options.EnableMessageExpiration = false;
            oCreation.Options.EnableHoldTransactionUntilMessageCommitted = false;
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

        protected sealed class ProducerScope : IDisposable
        {
            public QueueContainer<SqLiteMessageQueueInit> Creator { get; init; }
            public IProducerQueue<FakeMessage> Producer { get; init; }
            public IRelationalProducerQueue<FakeMessage> RelationalProducer { get; init; }

            private int _disposed;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
                Producer?.Dispose();
                Creator?.Dispose();
            }
        }

        protected ProducerScope CreateRelationalProducer(QueueConnection queueConnection)
        {
            var creator = new QueueContainer<SqLiteMessageQueueInit>();
            var producer = creator.CreateProducer<FakeMessage>(queueConnection);
            Assert.IsInstanceOfType<IRelationalProducerQueue<FakeMessage>>(producer,
                "SQLite producer must implement IRelationalProducerQueue<T> (PROJECT.md §SC #3)");
            var rp = (IRelationalProducerQueue<FakeMessage>)producer;
            return new ProducerScope { Creator = creator, Producer = producer, RelationalProducer = rp };
        }

        // ---- Business table lifecycle ----
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

        protected static void InsertBusinessRow(SQLiteConnection conn, SQLiteTransaction transaction, string tableName, int id, string val)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = $"INSERT INTO {tableName} (Id, Val) VALUES (@id, @val)";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@val", val);
            cmd.ExecuteNonQuery();
        }

        protected static long CountBusinessRows(SQLiteConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {tableName}";
            return Convert.ToInt64(cmd.ExecuteScalar());
        }

        protected static long CountQueueMessages(QueueConnection queueConnection)
        {
            using var conn = new SQLiteConnection(queueConnection.Connection);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {queueConnection.Queue}MetaData";
            return Convert.ToInt64(cmd.ExecuteScalar());
        }

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

        /// <summary>
        /// PROJECT.md §SC #8 zero-mutation behavioral pin. Asserts the caller's connection
        /// + transaction are still in a usable state after Send returns: connection still Open,
        /// transaction.Connection still non-null. The strongest follow-on proof — that the
        /// caller can still call <c>transaction.Commit()</c> without throwing — is exercised
        /// inline by every commit-path test in the suite.
        /// </summary>
        protected static void AssertCallerResourcesUnmutated(SQLiteConnection conn, SQLiteTransaction transaction)
        {
            Assert.AreEqual(ConnectionState.Open, conn.State,
                "caller's connection was mutated (no longer Open) by Send — violates §SC #8.");
            Assert.IsNotNull(transaction.Connection,
                "caller's transaction was disposed or invalidated by Send — violates §SC #8.");
        }
    }
}
