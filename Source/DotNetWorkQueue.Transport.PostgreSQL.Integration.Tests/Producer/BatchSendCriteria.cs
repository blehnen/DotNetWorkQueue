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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Npgsql;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Producer
{
    /// <summary>
    /// Focused coverage for the PostgreSQL true-batch send success criteria that the broad
    /// <see cref="SimpleProducerBatch"/> data-driven suite does not assert explicitly:
    /// id-to-input-order mapping, whole-batch atomic rollback, and a configured batch size.
    /// Requires a running PostgreSQL with <c>connectionstring.txt</c> configured.
    /// </summary>
    [TestClass]
    public class BatchSendCriteria
    {
        /// <summary>Criteria 1 (all rows), 2 (ids in input order), 4 (chunking across the boundary).</summary>
        [TestMethod]
        public void BatchSend_ReturnsIdsMappedToInputOrder_AcrossChunks()
        {
            // Derived from the transport safe-max so the batch always spans multiple chunks in one
            // transaction, even if SafeMaxBatchSize changes (one full chunk + a small second chunk).
            const int count = SendMessageBatch.SafeMaxBatchSize + 100;
            var queueName = GenerateQueueName.Create();
            var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);

            using (var queueCreator = new QueueCreationContainer<PostgreSqlMessageQueueInit>())
            using (var oCreation = queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection))
            {
                // OrderID lands on the meta table (which always exists) so we can map each
                // returned id back to the input message that carried that ordinal.
                oCreation.Options.AdditionalColumnsOnMetaData = true;
                oCreation.Options.AdditionalColumns.Add(new Column("OrderID", ColumnTypes.Integer, true));
                Assert.IsTrue(oCreation.CreateQueue().Success);
                try
                {
                    var nameHelper = new TableNameHelper(new SqlConnectionInformation(queueConnection));
                    using (var qc = new QueueContainer<PostgreSqlMessageQueueInit>())
                    using (var producer = qc.CreateProducer<FakeMessage>(queueConnection))
                    {
                        var messages = new List<QueueMessage<FakeMessage, IAdditionalMessageData>>(count);
                        for (var i = 0; i < count; i++)
                        {
                            var data = new AdditionalMessageData();
                            data.AdditionalMetaData.Add(new AdditionalMetaData<int>("OrderID", i));
                            messages.Add(new QueueMessage<FakeMessage, IAdditionalMessageData>(
                                new FakeMessage { Name = "msg-" + i }, data));
                        }

                        var output = producer.Send(messages);
                        Assert.IsFalse(output.HasErrors, "batch send reported errors");

                        var ids = output.Select(m => Convert.ToInt64(m.SentMessage.MessageId.Id.Value)).ToList();
                        Assert.AreEqual(count, ids.Count);
                        Assert.AreEqual(count, ids.Distinct().Count(), "generated ids are not unique");

                        // Definitive id-order check: the OrderID stored for result[i].id must equal i.
                        // Load the whole QueueID -> OrderID map in one query rather than per-row.
                        var orderById = LoadIntMap($"select QueueID, OrderID from {nameHelper.MetaDataName}");
                        for (var i = 0; i < count; i++)
                        {
                            Assert.IsTrue(orderById.TryGetValue(ids[i], out var orderId),
                                $"no meta row for id at result position {i}");
                            Assert.AreEqual(i, orderId, $"id at result position {i} maps to the wrong input message");
                        }
                    }
                }
                finally
                {
                    oCreation.RemoveQueue();
                }
            }
        }

        /// <summary>Criterion 3: a mid-batch failure rolls back the entire batch (no partial rows).</summary>
        [TestMethod]
        public void BatchSend_MidBatchFailure_RollsBackEntireBatch()
        {
            var queueName = GenerateQueueName.Create();
            var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);

            using (var queueCreator = new QueueCreationContainer<PostgreSqlMessageQueueInit>())
            using (var oCreation = queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection))
            {
                Assert.IsTrue(oCreation.CreateQueue().Success);
                try
                {
                    var nameHelper = new TableNameHelper(new SqlConnectionInformation(queueConnection));
                    // Drop the meta table so the per-message meta insert fails AFTER the body insert
                    // succeeds within the same transaction — the whole batch must roll back.
                    ExecuteNonQuery($"drop table {nameHelper.MetaDataName}");

                    using (var qc = new QueueContainer<PostgreSqlMessageQueueInit>())
                    using (var producer = qc.CreateProducer<FakeMessage>(queueConnection))
                    {
                        var messages = Enumerable.Range(0, 25)
                            .Select(i => new QueueMessage<FakeMessage, IAdditionalMessageData>(
                                new FakeMessage { Name = "msg-" + i }, null))
                            .ToList();

                        var output = producer.Send(messages);
                        Assert.IsTrue(output.HasErrors, "expected the batch to fail");
                        Assert.IsTrue(output.All(m => m.SendingException != null),
                            "every result should report the batch failure");
                    }

                    // Whole-batch atomic: no body rows were committed.
                    Assert.AreEqual(0, ScalarLong($"select count(*) from {nameHelper.QueueName}"),
                        "body rows were committed despite a mid-batch failure");
                }
                finally
                {
                    oCreation.RemoveQueue();
                }
            }
        }

        /// <summary>Criterion 5: a configured batch size is honored (all rows land across many small chunks).</summary>
        [TestMethod]
        public void BatchSend_HonorsConfiguredBatchSize()
        {
            const int count = 1000;
            var queueName = GenerateQueueName.Create();
            var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);

            using (var queueCreator = new QueueCreationContainer<PostgreSqlMessageQueueInit>())
            using (var oCreation = queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection))
            {
                oCreation.Options.BatchSize = 50; // small ceiling -> 20 chunks, all in one transaction
                Assert.IsTrue(oCreation.CreateQueue().Success);
                try
                {
                    using (var qc = new QueueContainer<PostgreSqlMessageQueueInit>())
                    using (var producer = qc.CreateProducer<FakeMessage>(queueConnection))
                    {
                        var messages = Enumerable.Range(0, count)
                            .Select(i => new QueueMessage<FakeMessage, IAdditionalMessageData>(
                                new FakeMessage { Name = "msg-" + i }, null))
                            .ToList();

                        var output = producer.Send(messages);
                        Assert.IsFalse(output.HasErrors);
                        Assert.AreEqual(count, output.Count);
                    }
                    new VerifyQueueData(queueName, oCreation.Options).Verify(count, null);
                }
                finally
                {
                    oCreation.RemoveQueue();
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Table names from configuration; values are literal ints from the test")]
        private static Dictionary<long, int> LoadIntMap(string sql)
        {
            var map = new Dictionary<long, int>();
            using (var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = sql;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            map[reader.GetInt64(0)] = Convert.ToInt32(reader.GetValue(1));
                    }
                }
            }
            return map;
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Table names from configuration; values are literal ints from the test")]
        private static void ExecuteNonQuery(string sql)
        {
            using (var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Table names from configuration; values are literal ints from the test")]
        private static long ScalarLong(string sql)
        {
            using (var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = sql;
                    return Convert.ToInt64(command.ExecuteScalar());
                }
            }
        }
    }
}
