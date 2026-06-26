using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.Schema;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Producer
{
    /// <summary>
    /// Focused coverage for the SQL Server true-batch send success criteria that the broad
    /// <see cref="SimpleProducerBatch"/> data-driven suite does not assert explicitly:
    /// id-to-input-order mapping, whole-batch atomic rollback, and a configured batch size.
    /// Requires a running SQL Server with <c>connectionstring.txt</c> configured.
    /// </summary>
    [TestClass]
    public class BatchSendCriteria
    {
        /// <summary>Criteria 1 (all rows), 2 (ids in input order), 4 (chunking across the boundary).</summary>
        [TestMethod]
        public void BatchSend_ReturnsIdsMappedToInputOrder_AcrossChunks()
        {
            // > SafeMaxBatchSize (~1000) so the batch spans multiple chunks in one transaction.
            const int count = 1500;
            var queueName = GenerateQueueName.Create();
            var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);

            using (var queueCreator = new QueueCreationContainer<SqlServerMessageQueueInit>())
            using (var oCreation = queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection))
            {
                // OrderID lands on the status table (additional columns, default placement) so we can
                // map each returned id back to the input message that carried that ordinal.
                oCreation.Options.AdditionalColumns.Add(new Column("OrderID", ColumnTypes.Int, true, null));
                oCreation.Options.AdditionalConstraints.Add(new Constraint("IX_OrderID", ConstraintType.Index, "OrderID"));
                Assert.IsTrue(oCreation.CreateQueue().Success);
                try
                {
                    var nameHelper = new SqlServerTableNameHelper(new SqlConnectionInformation(queueConnection));
                    using (var qc = new QueueContainer<SqlServerMessageQueueInit>())
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
                            var orderId = ScalarInt(queueConnection,
                                $"select OrderID from {nameHelper.StatusName} where QueueID = {ids[i]}");
                            Assert.AreEqual(i, orderId, $"id at result position {i} maps to the wrong input message");
                        }
                    }
                    new VerifyQueueData(queueConnection, oCreation.Options).Verify(count);
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

            using (var queueCreator = new QueueCreationContainer<SqlServerMessageQueueInit>())
            using (var oCreation = queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection))
            {
                Assert.IsTrue(oCreation.CreateQueue().Success);
                try
                {
                    var nameHelper = new SqlServerTableNameHelper(new SqlConnectionInformation(queueConnection));
                    // Drop the meta table so the per-message meta insert fails AFTER the body MERGE
                    // succeeds within the same transaction — the whole batch must roll back.
                    ExecuteNonQuery(queueConnection, $"drop table {nameHelper.MetaDataName}");

                    using (var qc = new QueueContainer<SqlServerMessageQueueInit>())
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
                    Assert.AreEqual(0, ScalarInt(queueConnection, $"select count(*) from {nameHelper.QueueName}"),
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

            using (var queueCreator = new QueueCreationContainer<SqlServerMessageQueueInit>())
            using (var oCreation = queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection))
            {
                oCreation.Options.BatchSize = 50; // small ceiling -> 20 chunks, all in one transaction
                Assert.IsTrue(oCreation.CreateQueue().Success);
                try
                {
                    using (var qc = new QueueContainer<SqlServerMessageQueueInit>())
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
                    new VerifyQueueData(queueConnection, oCreation.Options).Verify(count);
                }
                finally
                {
                    oCreation.RemoveQueue();
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Table names from configuration; values are literal ints from the test")]
        private static void ExecuteNonQuery(QueueConnection queueConnection, string sql)
        {
            using (var conn = new SqlConnection(new SqlConnectionInformation(queueConnection).ConnectionString))
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
        private static int ScalarInt(QueueConnection queueConnection, string sql)
        {
            using (var conn = new SqlConnection(new SqlConnectionInformation(queueConnection).ConnectionString))
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
