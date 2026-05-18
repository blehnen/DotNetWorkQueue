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
    /// Negative-path inbox tests for SqlServer. When the consumer is configured with
    /// <c>EnableHoldTransactionUntilMessageCommitted = false</c>, the capability cast
    /// to <see cref="IRelationalWorkerNotification"/> must fail cleanly — the handler
    /// observes a plain <c>WorkerNotification</c> and the cast pattern-match returns
    /// false. The handler can then surface a discoverable
    /// <see cref="InvalidOperationException"/> rather than the framework chain throwing
    /// an obscure <see cref="NullReferenceException"/>.
    /// </summary>
    [TestClass]
    public class SqlServerInboxOptionFalseTests : SqlServerInboxIntegrationTestBase
    {
        private const string ExpectedHandlerMessage =
            "Inbox capability requires EnableHoldTransactionUntilMessageCommitted = true";

        [ClassInitialize]
        public static void Init(TestContext _) => EnsureActivityListenerRegistered();

        [TestMethod]
        public void Sync_OptionFalse_CapabilityCastFails_DiscoverableError()
        {
            var connStr = ConnectionInfo.ConnectionString;
            var qc = new QueueConnection(NewQueueName(), connStr);

            using var queue = CreateQueue(qc, enableHoldTransaction: false);

            var handlerInvoked = new ManualResetEventSlim(false);
            Exception capturedException = null;

            using (var queueContainer = new QueueContainer<SqlServerMessageQueueInit>())
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
                        try
                        {
                            if (workerNotification is IRelationalWorkerNotification)
                            {
                                throw new AssertFailedException(
                                    "capability cast unexpectedly succeeded with option=false");
                            }
                            throw new InvalidOperationException(ExpectedHandlerMessage);
                        }
                        catch (Exception ex)
                        {
                            capturedException = ex;
                            handlerInvoked.Set();
                            throw;
                        }
                    }, null);

                    Assert.IsTrue(handlerInvoked.Wait(TimeSpan.FromSeconds(30)), "handler was not invoked within 30s");
                }
            }

            Assert.IsNotNull(capturedException, "handler did not run");
            Assert.IsInstanceOfType<InvalidOperationException>(capturedException,
                $"expected InvalidOperationException from handler; got {capturedException.GetType().Name}: {capturedException.Message}");
            Assert.IsNotInstanceOfType<NullReferenceException>(capturedException,
                "framework code path produced a NullReferenceException under option=false");
            StringAssert.Contains(capturedException.Message, ExpectedHandlerMessage);
        }

        [TestMethod]
        public void Async_OptionFalse_CapabilityCastFails_DiscoverableError()
        {
            var connStr = ConnectionInfo.ConnectionString;
            var qc = new QueueConnection(NewQueueName(), connStr);

            using var queue = CreateQueue(qc, enableHoldTransaction: false);

            var handlerInvoked = new ManualResetEventSlim(false);
            Exception capturedException = null;

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
                        try
                        {
                            if (workerNotification is IRelationalWorkerNotification)
                            {
                                throw new AssertFailedException(
                                    "capability cast unexpectedly succeeded with option=false");
                            }
                            throw new InvalidOperationException(ExpectedHandlerMessage);
                        }
                        catch (Exception ex)
                        {
                            capturedException = ex;
                            handlerInvoked.Set();
                            throw;
                        }
                    }, null);

                    Assert.IsTrue(handlerInvoked.Wait(TimeSpan.FromSeconds(30)), "async handler was not invoked within 30s");
                }
            }

            Assert.IsNotNull(capturedException, "async handler did not run");
            Assert.IsInstanceOfType<InvalidOperationException>(capturedException,
                $"expected InvalidOperationException from handler; got {capturedException.GetType().Name}: {capturedException.Message}");
            Assert.IsNotInstanceOfType<NullReferenceException>(capturedException,
                "framework code path produced a NullReferenceException under option=false");
            StringAssert.Contains(capturedException.Message, ExpectedHandlerMessage);
        }
    }
}
