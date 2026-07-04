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
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers;
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    /// <summary>
    /// Regression tests for the specific bug where the dashboard's transport options factory
    /// caches a default (<c>EnableHistory=false</c>) options instance when the queue's
    /// configuration table does not exist at first resolution. This cached default persists for
    /// the lifetime of the container — meaning a dashboard started before the queue exists
    /// would silently return empty history even after the queue is created with history enabled.
    ///
    /// These tests drive the startup-before-queue timing scenario against SQLite (simplest
    /// relational transport) and verify the dashboard returns history records anyway.
    /// The fix removed the <c>EnableHistory</c> guard from the read-path handlers, making
    /// reads resilient to stale/default options.
    /// </summary>
    [TestClass]
    public class DashboardStartupTimingTests
    {
        private const int MessageCount = 3;
        private DashboardTestServer _server;
        private string _queueName;
        private string _connStr;
        private ICreationScope _scope;
        private QueueCreationContainer<SqLiteMessageQueueInit> _creationContainer;
        private SqLiteMessageQueueCreation _creation;

        [TestInitialize]
        public void Initialize()
        {
            _queueName = QueueNameGenerator.Create();
            _connStr = ConnectionStrings.CreateSqliteInMemory(_queueName);
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (_server != null) await _server.DisposeAsync();
            try { _creation?.RemoveQueue(); } catch { /* best-effort */ }
            _creation?.Dispose();
            _creationContainer?.Dispose();
            _scope?.Dispose();
        }

        [TestMethod]
        public async Task Dashboard_Started_Before_Queue_Exists_Still_Returns_History_After_Queue_Creation()
        {
            // --- Phase 1: start dashboard BEFORE the queue is created ---
            // The options factory will attempt to load options from a non-existent configuration
            // table and cache a default instance with EnableHistory=false. Pre-fix, this caused
            // all subsequent history reads to silently short-circuit to empty even after data
            // was later written.
            _server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<SqLiteMessageQueueInit>(_connStr,
                    conn => conn.AddQueue(_queueName));
            });

            // Trigger the options factory to resolve against the still-missing config table
            // by hitting an endpoint that resolves the transport-options chain.
            var connections = await _server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            Assert.IsNotNull(connections);
            Assert.IsTrue(connections.Count > 0);
            var connectionId = connections[0].Id;

            var queues = await _server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connectionId}/queues");
            Assert.IsNotNull(queues);
            Assert.IsTrue(queues.Count > 0);
            var queueId = queues[0].Id;

            // --- Phase 2: create the queue with history enabled and populate it ---
            var queueConnection = new QueueConnection(_queueName, _connStr);
            _creationContainer = new QueueCreationContainer<SqLiteMessageQueueInit>();
            _creation = _creationContainer.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection);
            _creation.Options.EnableStatus = true;
            _creation.Options.EnableStatusTable = true;
            _creation.Options.EnableHistory = true;
            var createResult = _creation.CreateQueue();
            Assert.IsTrue(createResult.Success, createResult.ErrorMessage);
            _scope = _creation.Scope;

            using (var queueContainer = new QueueContainer<SqLiteMessageQueueInit>(
                       serviceRegister => serviceRegister.RegisterNonScopedSingleton(_scope)))
            {
                using (var producer = queueContainer.CreateProducer<FakeMessage>(queueConnection))
                {
                    for (var i = 0; i < MessageCount; i++)
                    {
                        var result = producer.Send(new FakeMessage());
                        Assert.IsFalse(result.HasError, $"Send failed: {result.SendingException?.Message}");
                    }
                }

                var processedCount = 0;
                var waitHandle = new ManualResetEventSlim(false);
                using (var consumer = queueContainer.CreateConsumer(queueConnection))
                {
                    consumer.Configuration.Worker.WorkerCount = 1;
                    consumer.Start<FakeMessage>((message, notifications) =>
                    {
                        if (Interlocked.Increment(ref processedCount) >= MessageCount)
                            waitHandle.Set();
                    }, new ConsumerQueueNotifications());

                    waitHandle.Wait(TimeSpan.FromSeconds(30));
                }

                Assert.AreEqual(MessageCount, processedCount, "Not all messages were processed");
            }

            // --- Phase 3: query history through the ORIGINAL dashboard container ---
            // Pre-fix this would return items=[] / count=0 because the options factory
            // cached EnableHistory=false at phase-1 resolution.
            var history = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{queueId}/history?pageSize=100");
            Assert.IsNotNull(history);
            Assert.IsTrue(history.Items.Count >= MessageCount,
                "history reads must survive a stale/default options cache on the dashboard side");

            var countResponse = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{queueId}/history/count");
            countResponse.EnsureSuccessStatusCode();
            var count = await countResponse.Content.ReadFromJsonAsync<long>();
            Assert.IsTrue(count >= MessageCount);
        }
    }
}
