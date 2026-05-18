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
using System.Data.SQLite;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Inbox
{
    /// <summary>
    /// Cross-connection atomic visibility tests for SQLite. Opens a fresh SQLiteConnection
    /// AFTER the consumer is fully disposed to confirm the queue transaction and business
    /// write commit (or roll back) atomically as observed from outside the library's
    /// own connection.
    /// </summary>
    [TestClass]
    public class SqLiteInboxAtomicVisibilityTests : SqLiteInboxIntegrationTestBase
    {
        [ClassInitialize]
        public static void Init(TestContext _) => EnsureActivityListenerRegistered();

        [TestMethod]
        public void BusinessRow_Visible_After_QueueCommit_FromSeparateConnection()
        {
            using var dbScope = new IntegrationConnectionInfo(inMemory: false);
            var connStr = dbScope.ConnectionString;
            var qc = new QueueConnection(NewQueueName(), connStr);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc, enableHoldTransaction: true);
            CreateBusinessTable(connStr, businessTable);
            try
            {
                var handlerInvoked = new ManualResetEventSlim(false);

                using (var queueContainer = new QueueContainer<SqLiteMessageQueueInit>())
                {
                    using (var producer = queueContainer.CreateProducer<FakeMessage>(qc))
                    {
                        var sendResult = producer.Send(new FakeMessage());
                        Assert.IsFalse(sendResult.HasError, sendResult.SendingException?.ToString());
                    }

                    using (var consumer = queueContainer.CreateConsumer(qc))
                    {
                        consumer.Configuration.Worker.WorkerCount = 1;
                        consumer.Start<FakeMessage>((message, workerNotification) =>
                        {
                            var relational = (IRelationalWorkerNotification)workerNotification;
                            InsertBusinessRowOnInboxTransaction(relational.Transaction, businessTable, 42, "atomic-commit");
                            handlerInvoked.Set();
                        }, null);

                        Assert.IsTrue(handlerInvoked.Wait(TimeSpan.FromSeconds(30)), "handler was not invoked within 30s");
                    }
                }

                using var verifyConn = new SQLiteConnection(connStr);
                verifyConn.Open();
                using var cmd = verifyConn.CreateCommand();
                cmd.CommandText = $"SELECT COUNT(*) FROM {businessTable} WHERE Id = 42";
                var count = Convert.ToInt64(cmd.ExecuteScalar());
                Assert.AreEqual(1L, count, "business row not visible from a separate connection after queue commit");
            }
            finally
            {
                DropBusinessTable(connStr, businessTable);
            }
        }

        [TestMethod]
        public void BusinessRow_NotVisible_After_QueueRollback_FromSeparateConnection()
        {
            using var dbScope = new IntegrationConnectionInfo(inMemory: false);
            var connStr = dbScope.ConnectionString;
            var qc = new QueueConnection(NewQueueName(), connStr);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc, enableHoldTransaction: true);
            CreateBusinessTable(connStr, businessTable);
            try
            {
                var handlerInvoked = new ManualResetEventSlim(false);

                using (var queueContainer = new QueueContainer<SqLiteMessageQueueInit>())
                {
                    using (var producer = queueContainer.CreateProducer<FakeMessage>(qc))
                    {
                        var sendResult = producer.Send(new FakeMessage());
                        Assert.IsFalse(sendResult.HasError, sendResult.SendingException?.ToString());
                    }

                    using (var consumer = queueContainer.CreateConsumer(qc))
                    {
                        consumer.Configuration.Worker.WorkerCount = 1;
                        consumer.Configuration.TransportConfiguration.RetryDelayBehavior.Add(
                            typeof(InvalidOperationException),
                            new System.Collections.Generic.List<TimeSpan>
                            {
                                TimeSpan.FromMilliseconds(100)
                            });

                        consumer.Start<FakeMessage>((message, workerNotification) =>
                        {
                            var relational = (IRelationalWorkerNotification)workerNotification;
                            InsertBusinessRowOnInboxTransaction(relational.Transaction, businessTable, 99, "atomic-rollback");
                            handlerInvoked.Set();
                            throw new InvalidOperationException("intentional throw — should roll back inbox transaction");
                        }, null);

                        Assert.IsTrue(handlerInvoked.Wait(TimeSpan.FromSeconds(30)), "handler was not invoked within 30s");
                    }
                }

                Thread.Sleep(1500);
                using var verifyConn = new SQLiteConnection(connStr);
                verifyConn.Open();
                using var cmd = verifyConn.CreateCommand();
                cmd.CommandText = $"SELECT COUNT(*) FROM {businessTable} WHERE Id = 99";
                var count = Convert.ToInt64(cmd.ExecuteScalar());
                Assert.AreEqual(0L, count, "business row should not be visible from a separate connection after queue rollback");
            }
            finally
            {
                DropBusinessTable(connStr, businessTable);
            }
        }
    }
}
