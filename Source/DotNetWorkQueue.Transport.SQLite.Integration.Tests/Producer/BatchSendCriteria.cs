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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Producer
{
    /// <summary>
    /// Focused coverage for the SQLite true-batch send success criteria that the broad
    /// <see cref="SimpleProducerBatch"/> data-driven suite does not assert explicitly:
    /// id-to-input-order mapping, whole-batch atomic rollback, and a configured batch size.
    /// SQLite is embedded, so these run locally with no external service.
    /// </summary>
    [TestClass]
    public class BatchSendCriteria
    {
        /// <summary>Criteria 1 (all rows), 2 (ids in input order), 4 (chunking across the boundary).</summary>
        [TestMethod]
        public void BatchSend_ReturnsIdsMappedToInputOrder_AcrossChunks()
        {
            // > SafeMaxBatchSize (~450) so the batch spans multiple chunks in one transaction.
            const int count = 1500;
            using (var connectionInfo = new IntegrationConnectionInfo(false))
            {
                var queueName = GenerateQueueName.Create();
                var queueConnection = new QueueConnection(queueName, connectionInfo.ConnectionString);

                using (var queueCreator = new QueueCreationContainer<SqLiteMessageQueueInit>())
                using (var oCreation = queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection))
                {
                    // OrderID lands on the meta table (which always exists) so we can map each
                    // returned id back to the input message that carried that ordinal.
                    oCreation.Options.AdditionalColumnsOnMetaData = true;
                    oCreation.Options.AdditionalColumns.Add(new Column("OrderID", ColumnTypes.Integer, true, null));
                    Assert.IsTrue(oCreation.CreateQueue().Success);
                    try
                    {
                        var nameHelper = new TableNameHelper(
                            new SqliteConnectionInformation(queueConnection, new DbDataSource()));
                        using (var qc = new QueueContainer<SqLiteMessageQueueInit>())
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

                            // Definitive id-order check: the OrderID stored for result[i].id must equal i,
                            // i.e. each returned id maps to the input message at that position.
                            for (var i = 0; i < count; i++)
                            {
                                var orderId = ScalarInt(connectionInfo.ConnectionString,
                                    $"select OrderID from {nameHelper.MetaDataName} where QueueID = {ids[i]}");
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
        }

        /// <summary>Criterion 3: a mid-batch failure rolls back the entire batch (no partial rows).</summary>
        [TestMethod]
        public void BatchSend_MidBatchFailure_RollsBackEntireBatch()
        {
            using (var connectionInfo = new IntegrationConnectionInfo(false))
            {
                var queueName = GenerateQueueName.Create();
                var queueConnection = new QueueConnection(queueName, connectionInfo.ConnectionString);

                using (var queueCreator = new QueueCreationContainer<SqLiteMessageQueueInit>())
                using (var oCreation = queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection))
                {
                    Assert.IsTrue(oCreation.CreateQueue().Success);
                    try
                    {
                        var nameHelper = new TableNameHelper(
                            new SqliteConnectionInformation(queueConnection, new DbDataSource()));
                        // Drop the meta table so the per-message meta insert fails AFTER the body insert
                        // succeeds within the same transaction — the whole batch must roll back.
                        ExecuteNonQuery(connectionInfo.ConnectionString, $"drop table {nameHelper.MetaDataName}");

                        using (var qc = new QueueContainer<SqLiteMessageQueueInit>())
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
                        Assert.AreEqual(0, ScalarInt(connectionInfo.ConnectionString,
                            $"select count(*) from {nameHelper.QueueName}"),
                            "body rows were committed despite a mid-batch failure");
                    }
                    finally
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }

        /// <summary>Criterion 5: a configured batch size is honored (all rows land across many small chunks).</summary>
        [TestMethod]
        public void BatchSend_HonorsConfiguredBatchSize()
        {
            const int count = 1000;
            using (var connectionInfo = new IntegrationConnectionInfo(false))
            {
                var queueName = GenerateQueueName.Create();
                var queueConnection = new QueueConnection(queueName, connectionInfo.ConnectionString);

                using (var queueCreator = new QueueCreationContainer<SqLiteMessageQueueInit>())
                using (var oCreation = queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection))
                {
                    oCreation.Options.BatchSize = 50; // small ceiling -> 20 chunks, all in one transaction
                    Assert.IsTrue(oCreation.CreateQueue().Success);
                    try
                    {
                        using (var qc = new QueueContainer<SqLiteMessageQueueInit>())
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
                        new VerifyQueueData(queueConnection, oCreation.Options).Verify(count, null);
                    }
                    finally
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Table names from configuration; values are literal ints from the test")]
        private static void ExecuteNonQuery(string connectionString, string sql)
        {
            using (var conn = new SQLiteConnection(connectionString))
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
        private static int ScalarInt(string connectionString, string sql)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = sql;
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }
    }
}
