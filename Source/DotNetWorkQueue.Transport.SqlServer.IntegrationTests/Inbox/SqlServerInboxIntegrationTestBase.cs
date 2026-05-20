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
using System.Diagnostics;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Inbox
{
    /// <summary>
    /// Shared base for SqlServer inbox-pattern integration tests.
    /// Owns queue lifecycle, business-table lifecycle, consumer plumbing,
    /// and ActivityListener registration so trace decorator paths execute
    /// during integration runs (CLAUDE.md trace-decorator coverage lesson).
    /// </summary>
    public abstract class SqlServerInboxIntegrationTestBase
    {
        // ---- Queue name (CLAUDE.md lesson: DNQ rejects hyphenated GUIDs) ----
        protected static string NewQueueName() => "q" + Guid.NewGuid().ToString("N");

        // ---- Business table name (parallel-safe; one per test) ----
        protected static string NewBusinessTableName() => "InboxBusiness_" + Guid.NewGuid().ToString("N");

        // ---- ActivityListener (mandatory per CLAUDE.md trace-decorator lesson) ----
        private static readonly object ListenerLock = new();
        private static ActivityListener _listener;

        /// <summary>
        /// Idempotently registers an ActivityListener for every DotNetWorkQueue ActivitySource so that
        /// trace decorators execute their full code path during the integration run. Without a listener
        /// ActivitySource.StartActivity returns null and the decorator chain silently short-circuits,
        /// dropping line coverage to 0% on the trace decorators.
        /// </summary>
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

        // ---- Queue creation / removal ----
        protected sealed class QueueScope : IDisposable
        {
            public QueueCreationContainer<SqlServerMessageQueueInit> QueueCreator { get; init; }
            public SqlServerMessageQueueCreation OCreation { get; init; }

            private int _disposed;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
                try { OCreation?.RemoveQueue(); } catch { /* swallow — teardown */ }
                OCreation?.Dispose();
                QueueCreator?.Dispose();
            }
        }

        protected QueueScope CreateQueue(QueueConnection queueConnection, bool enableHoldTransaction)
        {
            var queueCreator = new QueueCreationContainer<SqlServerMessageQueueInit>();
            var oCreation = queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection);

            // EnableStatus must be false when EnableHoldTransactionUntilMessageCommitted is true
            // (queue validation rejects the combination). Status tracking is not required for
            // any inbox-test assertion, so we leave it off in both modes for consistency.
            oCreation.Options.EnableStatus = false;
            oCreation.Options.EnableStatusTable = false;
            oCreation.Options.EnableHeartBeat = false;
            oCreation.Options.EnableDelayedProcessing = false;
            oCreation.Options.EnableMessageExpiration = false;
            oCreation.Options.EnableHoldTransactionUntilMessageCommitted = enableHoldTransaction;
            oCreation.Options.EnablePriority = false;

            var result = oCreation.CreateQueue();
            Assert.IsTrue(result.Success, result.ErrorMessage);

            return new QueueScope
            {
                QueueCreator = queueCreator,
                OCreation = oCreation
            };
        }

        // ---- Business table lifecycle (separate connection — not the consumer's transaction) ----
        protected static void CreateBusinessTable(string connectionString, string tableName)
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE TABLE dbo.{tableName} (Id INT NOT NULL, Val NVARCHAR(100) NOT NULL)";
            cmd.ExecuteNonQuery();
        }

        protected static void DropBusinessTable(string connectionString, string tableName)
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"IF OBJECT_ID('dbo.{tableName}', 'U') IS NOT NULL DROP TABLE dbo.{tableName}";
            cmd.ExecuteNonQuery();
        }

        protected static int CountBusinessRowsFromSeparateConnection(string connectionString, string tableName)
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM dbo.{tableName}";
            return (int)cmd.ExecuteScalar();
        }

        /// <summary>
        /// Polls the business table from a fresh connection until the count reaches <paramref name="expected"/>
        /// or the timeout elapses. Polling instead of snapshot — the inbox transaction commits asynchronously
        /// relative to the test thread (CLAUDE.md polling lesson).
        /// </summary>
        protected static void AssertBusinessRowCountFromSeparateConnection(
            string connectionString, string tableName, int expected, int timeoutMs = 5000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            int actual = -1;
            while (DateTime.UtcNow < deadline)
            {
                actual = CountBusinessRowsFromSeparateConnection(connectionString, tableName);
                if (actual == expected) return;
                Thread.Sleep(100);
            }
            Assert.AreEqual(expected, actual,
                $"Business table {tableName} expected {expected} rows, observed {actual} within {timeoutMs}ms.");
        }

        /// <summary>
        /// Asserts the business table row count remains at <paramref name="expected"/> for the full
        /// settle window — used for rollback paths where any commit would be observable from a
        /// separate connection within ~1s.
        /// </summary>
        protected static void AssertBusinessRowCountStaysAt(
            string connectionString, string tableName, int expected, int settleMs = 1500)
        {
            Thread.Sleep(settleMs);
            var actual = CountBusinessRowsFromSeparateConnection(connectionString, tableName);
            Assert.AreEqual(expected, actual,
                $"Business table {tableName} expected {expected} rows after {settleMs}ms settle, observed {actual}.");
        }

        // ---- INSERT into business table on the inbox transaction ----
        protected static void InsertBusinessRowOnInboxTransaction(
            System.Data.Common.DbTransaction inboxTransaction, string tableName, int id, string val)
        {
            using var cmd = inboxTransaction.Connection.CreateCommand();
            cmd.Transaction = inboxTransaction;
            cmd.CommandText = $"INSERT INTO dbo.{tableName} (Id, Val) VALUES (@id, @val)";
            var pId = cmd.CreateParameter();
            pId.ParameterName = "@id";
            pId.Value = id;
            cmd.Parameters.Add(pId);
            var pVal = cmd.CreateParameter();
            pVal.ParameterName = "@val";
            pVal.Value = val;
            cmd.Parameters.Add(pVal);
            cmd.ExecuteNonQuery();
        }
    }
}
