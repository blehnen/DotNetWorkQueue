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
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers;
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    /// <summary>
    /// Integration tests for dashboard history endpoints when history is ENABLED on a PostgreSQL
    /// transport. Messages are sent and consumed to completion so that history records are
    /// populated in the <c>{queue}History</c> table.
    ///
    /// Companion to <see cref="PostgreSqlHistoryTests"/> (history-disabled case).
    ///
    /// Existed to regression-proof the dashboard bug where reads were gated on
    /// <c>IBaseTransportOptions.EnableHistory</c>, causing empty results when the dashboard's
    /// cached options reported false despite the history table containing data.
    /// </summary>
    [TestClass]
    public class PostgreSqlHistoryEnabledTests
    {
        private const int MessageCount = 5;
        private DashboardTestServer _server;
        private string _queueName;
        private string _connStr;
        private ICreationScope _scope;
        private QueueCreationContainer<PostgreSqlMessageQueueInit> _creationContainer;
        private PostgreSqlMessageQueueCreation _creation;
        private Guid _queueId;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            _queueName = QueueNameGenerator.Create();
            _connStr = ConnectionStrings.PostgreSql;
            var queueConnection = new QueueConnection(_queueName, _connStr);

            // Create queue with history enabled
            _creationContainer = new QueueCreationContainer<PostgreSqlMessageQueueInit>();
            _creation = _creationContainer.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
            _creation.Options.EnableStatus = true;
            _creation.Options.EnableStatusTable = true;
            _creation.Options.EnableHistory = true;
            var createResult = _creation.CreateQueue();
            Assert.IsTrue(createResult.Success, createResult.ErrorMessage);
            _scope = _creation.Scope;

            // Send and consume messages to completion (generates history records in Complete state)
            using (var queueContainer = new QueueContainer<PostgreSqlMessageQueueInit>(
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

            // Start dashboard server — simulates normal flow where dashboard is started
            // AFTER the queue exists.
            _server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<PostgreSqlMessageQueueInit>(_connStr,
                    conn => conn.AddQueue(_queueName));
            });

            var connections = await _server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await _server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            _queueId = queues[0].Id;
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
        public async Task History_Returns_Records_When_Enabled()
        {
            var result = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history?pageSize=100");

            result.Should().NotBeNull();
            result.Items.Should().NotBeEmpty();
            result.Items.Count.Should().BeGreaterThanOrEqualTo(MessageCount);
        }

        [TestMethod]
        public async Task HistoryCount_NoFilter_Returns_AtLeast_MessageCount()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/history/count");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var count = await response.Content.ReadFromJsonAsync<long>();
            count.Should().BeGreaterThanOrEqualTo(MessageCount);
        }

        [TestMethod]
        public async Task History_Filtered_By_Complete_Status_Returns_Records()
        {
            // Status 2 = Complete — all messages have been processed to completion
            var result = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history?status=2&pageSize=100");

            result.Should().NotBeNull();
            result.Items.Should().NotBeEmpty();
            result.Items.Should().AllSatisfy(item => item.Status.Should().Be(2));
        }

        [TestMethod]
        public async Task History_Records_Have_Expected_Fields()
        {
            var result = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history?pageSize=1");

            result.Items.Should().NotBeEmpty();
            var record = result.Items[0];

            record.QueueId.Should().NotBeNullOrEmpty();
            record.Status.Should().Be(2); // Complete
            record.EnqueuedUtc.Should().BeAfter(DateTime.MinValue);
        }

        [TestMethod]
        public async Task PurgeHistory_WithDateFilter_Removes_Records()
        {
            // Purge records older than 0 days = purge all
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{_queueId}/history?olderThanDays=0");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<DeleteAllResponse>();
            result.Deleted.Should().BeGreaterThanOrEqualTo(MessageCount);

            // Verify count is now 0
            var countResponse = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/history/count");
            var count = await countResponse.Content.ReadFromJsonAsync<long>();
            count.Should().Be(0);
        }
    }
}
