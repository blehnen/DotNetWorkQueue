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
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Inbox
{
    /// <summary>
    /// Async inbox-pattern integration tests for SqlServer. Mirrors the sync suite but
    /// exercises the IConsumerQueueAsync path.
    /// </summary>
    [TestClass]
    public class SqlServerInboxAsyncHandlerTests : SqlServerInboxIntegrationTestBase
    {
        [ClassInitialize]
        public static void Init(TestContext _) => EnsureActivityListenerRegistered();

        [TestMethod]
        public void Async_Commit_BothRowsVisible()
        {
            var connStr = ConnectionInfo.ConnectionString;
            var qc = new QueueConnection(NewQueueName(), connStr);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc, enableHoldTransaction: true);
            CreateBusinessTable(connStr, businessTable);
            try
            {
                var handlerInvoked = new ManualResetEventSlim(false);
                Exception capturedException = null;
                var castSucceeded = false;

                using (var queueContainer = new QueueContainer<SqlServerMessageQueueInit>())
                {
                    using (var producer = queueContainer.CreateProducer<FakeMessage>(qc))
                    {
                        var sendResult = producer.Send(new FakeMessage());
                        Assert.IsFalse(sendResult.HasError, sendResult.SendingException?.ToString());
                    }

                    using (var consumer = queueContainer.CreateConsumerAsync(qc))
                    {
                        consumer.Configuration.Worker.WorkerCount = 1;
                        consumer.Start<FakeMessage>((message, workerNotification) =>
                        {
                            try
                            {
                                if (workerNotification is IRelationalWorkerNotification relational)
                                {
                                    castSucceeded = true;
                                    InsertBusinessRowOnInboxTransaction(relational.Transaction, businessTable, 1, "commit-async");
                                }
                            }
                            catch (Exception ex)
                            {
                                capturedException = ex;
                                throw;
                            }
                            finally
                            {
                                handlerInvoked.Set();
                            }
                            return Task.CompletedTask;
                        }, null);

                        Assert.IsTrue(handlerInvoked.Wait(TimeSpan.FromSeconds(30)), "async handler was not invoked within 30s");
                    }
                }

                Assert.IsNull(capturedException, $"async handler threw unexpectedly: {capturedException}");
                Assert.IsTrue(castSucceeded, "capability cast to IRelationalWorkerNotification failed when option=true");
                AssertBusinessRowCountFromSeparateConnection(connStr, businessTable, 1);
            }
            finally
            {
                DropBusinessTable(connStr, businessTable);
            }
        }

        [TestMethod]
        public void Async_Rollback_NeitherRowVisible()
        {
            var connStr = ConnectionInfo.ConnectionString;
            var qc = new QueueConnection(NewQueueName(), connStr);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc, enableHoldTransaction: true);
            CreateBusinessTable(connStr, businessTable);
            try
            {
                var handlerInvoked = new ManualResetEventSlim(false);
                var castSucceeded = false;

                using (var queueContainer = new QueueContainer<SqlServerMessageQueueInit>())
                {
                    using (var producer = queueContainer.CreateProducer<FakeMessage>(qc))
                    {
                        var sendResult = producer.Send(new FakeMessage());
                        Assert.IsFalse(sendResult.HasError, sendResult.SendingException?.ToString());
                    }

                    using (var consumer = queueContainer.CreateConsumerAsync(qc))
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
                            if (workerNotification is IRelationalWorkerNotification relational)
                            {
                                castSucceeded = true;
                                InsertBusinessRowOnInboxTransaction(relational.Transaction, businessTable, 1, "rollback-async");
                            }
                            handlerInvoked.Set();
                            throw new InvalidOperationException("intentional throw — should roll back inbox transaction");
                        }, null);

                        Assert.IsTrue(handlerInvoked.Wait(TimeSpan.FromSeconds(30)), "async handler was not invoked within 30s");
                    }
                }

                Assert.IsTrue(castSucceeded, "capability cast to IRelationalWorkerNotification failed when option=true");
                AssertBusinessRowCountStaysAt(connStr, businessTable, 0);
            }
            finally
            {
                DropBusinessTable(connStr, businessTable);
            }
        }
    }
}
