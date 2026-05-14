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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Outbox
{
    /// <summary>
    /// Shared base for SqlServer outbox integration tests.
    /// Owns queue lifecycle, producer resolution + capability cast, business-table
    /// lifecycle, and atomic-commit assertion helpers.
    /// </summary>
    public abstract class SqlServerOutboxIntegrationTestBase
    {
        // ---- Queue name (CLAUDE.md lesson: DNQ rejects hyphenated GUIDs) ----
        protected static string NewQueueName() => "q" + Guid.NewGuid().ToString("N");

        // ---- Business table name (parallel-safe; one per test) ----
        protected static string NewBusinessTableName() => "OutboxBusiness_" + Guid.NewGuid().ToString("N");

        // ---- Queue creation / removal ----
        protected sealed class QueueScope : IDisposable
        {
            public QueueCreationContainer<SqlServerMessageQueueInit> QueueCreator { get; init; }
            public SqlServerMessageQueueCreation OCreation { get; init; }

            public void Dispose()
            {
                try { OCreation?.RemoveQueue(); } catch { /* swallow — teardown */ }
                OCreation?.Dispose();
                QueueCreator?.Dispose();
            }
        }

        protected QueueScope CreateQueue(QueueConnection queueConnection)
        {
            var queueCreator = new QueueCreationContainer<SqlServerMessageQueueInit>();
            var oCreation = queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection);

            // Default options: keep tables minimal so MetaData row count cleanly equals
            // the number of messages enqueued. Status column on, status table off.
            oCreation.Options.EnableStatus = true;
            oCreation.Options.EnableStatusTable = false;
            oCreation.Options.EnableHeartBeat = false;
            oCreation.Options.EnableDelayedProcessing = false;
            oCreation.Options.EnableMessageExpiration = false;
            oCreation.Options.EnableHoldTransactionUntilMessageCommitted = false;
            oCreation.Options.EnablePriority = false;

            var result = oCreation.CreateQueue();
            Assert.IsTrue(result.Success, result.ErrorMessage);

            return new QueueScope
            {
                QueueCreator = queueCreator,
                OCreation = oCreation
            };
        }

        // ---- Producer resolution + capability cast ----
        protected sealed class ProducerScope : IDisposable
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

        protected ProducerScope CreateRelationalProducer(QueueConnection queueConnection)
        {
            var creator = new QueueContainer<SqlServerMessageQueueInit>();
            var producer = creator.CreateProducer<FakeMessage>(queueConnection);
            Assert.IsInstanceOfType(producer, typeof(IRelationalProducerQueue<FakeMessage>),
                "SqlServer producer must implement IRelationalProducerQueue<T> (PROJECT.md §SC #3)");
            var rp = (IRelationalProducerQueue<FakeMessage>)producer;
            return new ProducerScope { Creator = creator, Producer = producer, RelationalProducer = rp };
        }

        // ---- Business table lifecycle ----
        protected static void CreateBusinessTable(SqlConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE TABLE dbo.{tableName} (Id INT NOT NULL, Val NVARCHAR(100) NOT NULL)";
            cmd.ExecuteNonQuery();
        }

        protected static void DropBusinessTable(SqlConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"IF OBJECT_ID('dbo.{tableName}', 'U') IS NOT NULL DROP TABLE dbo.{tableName}";
            cmd.ExecuteNonQuery();
        }

        protected static void InsertBusinessRow(SqlConnection conn, SqlTransaction tx, string tableName, int id, string val)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = $"INSERT INTO dbo.{tableName} (Id, Val) VALUES (@id, @val)";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@val", val);
            cmd.ExecuteNonQuery();
        }

        protected static int CountBusinessRows(SqlConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM dbo.{tableName}";
            return (int)cmd.ExecuteScalar();
        }

        // ---- Queue row count assertion (polling, NOT snapshot — CLAUDE.md lesson) ----
        protected static int CountQueueMessages(QueueConnection queueConnection)
        {
            var info = new SqlConnectionInformation(queueConnection);
            var helper = new SqlServerTableNameHelper(info);
            using var conn = new SqlConnection(queueConnection.Connection);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {helper.MetaDataName}";
            return (int)cmd.ExecuteScalar();
        }

        /// <summary>Polls the queue MetaData row count until it equals expected, or times out.</summary>
        protected static void AssertQueueRowCount(QueueConnection queueConnection, int expected, int timeoutMs = 5000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            int actual = -1;
            while (DateTime.UtcNow < deadline)
            {
                actual = CountQueueMessages(queueConnection);
                if (actual == expected) return;
                System.Threading.Thread.Sleep(100);
            }
            Assert.AreEqual(expected, actual,
                $"Queue row count did not converge to {expected} within {timeoutMs}ms (last observed: {actual}).");
        }

        protected static void AssertBusinessRowExists(SqlConnection conn, string tableName, int expectedCount)
        {
            var actual = CountBusinessRows(conn, tableName);
            Assert.AreEqual(expectedCount, actual,
                $"Business table {tableName} expected {expectedCount} rows, observed {actual}.");
        }

        // ---- Convenience: build a batch ----
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
