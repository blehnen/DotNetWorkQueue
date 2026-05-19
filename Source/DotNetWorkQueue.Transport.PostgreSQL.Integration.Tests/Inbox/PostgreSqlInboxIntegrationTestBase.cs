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
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Inbox
{
    /// <summary>
    /// Shared base for PostgreSQL inbox-pattern integration tests. Mirrors the SqlServer
    /// inbox test base with Npgsql substituted and PG identifier casing applied
    /// (PostgreSQL folds unquoted identifiers to lowercase).
    /// </summary>
    public abstract class PostgreSqlInboxIntegrationTestBase
    {
        protected static string NewQueueName() => "q" + Guid.NewGuid().ToString("N");

        // PostgreSQL identifier max length is 63; "inboxbusiness_" + 32-char hex = 46. OK.
        protected static string NewBusinessTableName() => "inboxbusiness_" + Guid.NewGuid().ToString("N");

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
            public QueueCreationContainer<PostgreSqlMessageQueueInit> QueueCreator { get; init; }
            public PostgreSqlMessageQueueCreation OCreation { get; init; }
            public ICreationScope Scope { get; init; }

            public void Dispose()
            {
                try { OCreation?.RemoveQueue(); } catch { /* swallow */ }
                OCreation?.Dispose();
                Scope?.Dispose();
                QueueCreator?.Dispose();
            }
        }

        protected QueueScope CreateQueue(QueueConnection queueConnection, bool enableHoldTransaction)
        {
            var queueCreator = new QueueCreationContainer<PostgreSqlMessageQueueInit>();
            var oCreation = queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
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
                OCreation = oCreation,
                Scope = oCreation.Scope
            };
        }

        protected static void CreateBusinessTable(string connectionString, string tableName)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE TABLE {tableName} (Id INT NOT NULL, Val VARCHAR(100) NOT NULL)";
            cmd.ExecuteNonQuery();
        }

        protected static void DropBusinessTable(string connectionString, string tableName)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"DROP TABLE IF EXISTS {tableName}";
            cmd.ExecuteNonQuery();
        }

        protected static long CountBusinessRowsFromSeparateConnection(string connectionString, string tableName)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {tableName}";
            return (long)cmd.ExecuteScalar();
        }

        protected static void AssertBusinessRowCountFromSeparateConnection(
            string connectionString, string tableName, long expected, int timeoutMs = 5000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            long actual = -1;
            while (DateTime.UtcNow < deadline)
            {
                actual = CountBusinessRowsFromSeparateConnection(connectionString, tableName);
                if (actual == expected) return;
                Thread.Sleep(100);
            }
            Assert.AreEqual(expected, actual,
                $"Business table {tableName} expected {expected} rows, observed {actual} within {timeoutMs}ms.");
        }

        protected static void AssertBusinessRowCountStaysAt(
            string connectionString, string tableName, long expected, int settleMs = 1500)
        {
            Thread.Sleep(settleMs);
            var actual = CountBusinessRowsFromSeparateConnection(connectionString, tableName);
            Assert.AreEqual(expected, actual,
                $"Business table {tableName} expected {expected} rows after {settleMs}ms settle, observed {actual}.");
        }

        protected static void InsertBusinessRowOnInboxTransaction(
            System.Data.Common.DbTransaction inboxTransaction, string tableName, int id, string val)
        {
            using var cmd = inboxTransaction.Connection.CreateCommand();
            cmd.Transaction = inboxTransaction;
            cmd.CommandText = $"INSERT INTO {tableName} (Id, Val) VALUES (@id, @val)";
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
