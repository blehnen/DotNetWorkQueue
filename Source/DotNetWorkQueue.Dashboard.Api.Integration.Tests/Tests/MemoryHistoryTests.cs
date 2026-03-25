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
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    /// <summary>
    /// Tests for the history API endpoints when history is NOT enabled on a Memory transport.
    /// The NoOp handlers should return empty results.
    /// </summary>
    [TestClass]
    public class MemoryHistoryDisabledTests
    {
        private DashboardTestServer _server;
        private TransportFixture<MemoryDashboardInit, MessageQueueCreation> _fixture;
        private Guid _queueId;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.Memory;

            _fixture = new TransportFixture<MemoryDashboardInit, MessageQueueCreation>(
                queueName, connStr);

            _fixture.SendMessages<FakeMessage>(3);

            _server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<MemoryDashboardInit>(connStr,
                    serviceRegister => serviceRegister.RegisterNonScopedSingleton(_fixture.Scope),
                    conn => conn.AddQueue(queueName));
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
            _fixture?.Dispose();
        }

        [TestMethod]
        public async Task History_Returns_Empty_When_Not_Enabled()
        {
            var result = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history");

            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
        }

        [TestMethod]
        public async Task HistoryCount_Returns_Zero_When_Not_Enabled()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/history/count");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var count = await response.Content.ReadFromJsonAsync<long>();
            count.Should().Be(0);
        }

        [TestMethod]
        public async Task HistoryByMessageId_Returns_NotFound_When_Not_Enabled()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/history/99999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task PurgeHistory_Returns_Zero_When_Not_Enabled()
        {
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{_queueId}/history");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<DeleteAllResponse>();
            result.Deleted.Should().Be(0);
        }
    }

    /// <summary>
    /// Tests for the history API endpoints when history IS enabled on a Memory transport.
    /// Messages are sent and consumed to completion so that history records are populated.
    /// </summary>
    [TestClass]
    public class MemoryHistoryEnabledTests
    {
        private const int MessageCount = 5;
        private DashboardTestServer _server;
        private string _queueName;
        private ICreationScope _scope;
        private QueueCreationContainer<MemoryDashboardInit> _creationContainer;
        private MessageQueueCreation _creation;
        private Guid _queueId;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            _queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.Memory;
            var queueConnection = new QueueConnection(_queueName, connStr);

            // Create queue with history enabled
            _creationContainer = new QueueCreationContainer<MemoryDashboardInit>();
            _creation = _creationContainer.GetQueueCreation<MessageQueueCreation>(queueConnection);
            _creation.Options.EnableHistory = true;
            var createResult = _creation.CreateQueue();
            Assert.IsTrue(createResult.Success, createResult.ErrorMessage);
            _scope = _creation.Scope;

            // Send and consume messages to completion (generates history records)
            using (var queueContainer = new QueueContainer<MemoryDashboardInit>(
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

            // Start Dashboard server
            _server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<MemoryDashboardInit>(connStr,
                    serviceRegister => serviceRegister.RegisterNonScopedSingleton(_scope),
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
        public async Task History_Pagination_Page0()
        {
            var result = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history?pageIndex=0&pageSize=2");

            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().BeGreaterThanOrEqualTo(MessageCount);
            result.PageIndex.Should().Be(0);
            result.PageSize.Should().Be(2);
        }

        [TestMethod]
        public async Task History_Pagination_Page1()
        {
            var result = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history?pageIndex=1&pageSize=2");

            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.PageIndex.Should().Be(1);
        }

        [TestMethod]
        public async Task History_Pagination_BeyondLast_Returns_Empty()
        {
            var result = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history?pageIndex=100&pageSize=25");

            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().BeGreaterThanOrEqualTo(MessageCount);
        }

        [TestMethod]
        public async Task History_Filtered_By_Complete_Status()
        {
            // Status 2 = Complete
            var result = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history?status=2&pageSize=100");

            result.Should().NotBeNull();
            result.Items.Should().NotBeEmpty();
            result.Items.Should().AllSatisfy(item => item.Status.Should().Be(2));
        }

        [TestMethod]
        public async Task History_Filtered_By_Error_Status_Returns_Empty()
        {
            // Status 3 = Error -- no errors in this test
            var result = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history?status=3&pageSize=100");

            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
        }

        [TestMethod]
        public async Task History_Filtered_By_Processing_Status_Returns_Empty()
        {
            // Status 1 = Processing -- all messages have completed
            var result = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history?status=1&pageSize=100");

            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
        }

        [TestMethod]
        public async Task HistoryCount_NoFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/history/count");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var count = await response.Content.ReadFromJsonAsync<long>();
            count.Should().BeGreaterThanOrEqualTo(MessageCount);
        }

        [TestMethod]
        public async Task HistoryCount_WithCompleteStatusFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/history/count?status=2");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var count = await response.Content.ReadFromJsonAsync<long>();
            count.Should().BeGreaterThanOrEqualTo(MessageCount);
        }

        [TestMethod]
        public async Task HistoryCount_WithErrorStatusFilter_Returns_Zero()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/history/count?status=3");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var count = await response.Content.ReadFromJsonAsync<long>();
            count.Should().Be(0);
        }

        [TestMethod]
        public async Task HistoryByQueueId_Returns_Record()
        {
            var history = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history?pageSize=1");
            history.Items.Should().NotBeEmpty();
            var recordQueueId = history.Items[0].QueueId;

            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/history/{recordQueueId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var record = await response.Content.ReadFromJsonAsync<HistoryResponse>();
            record.Should().NotBeNull();
            record.QueueId.Should().Be(recordQueueId);
            record.Status.Should().Be(2); // Complete
        }

        [TestMethod]
        public async Task HistoryByQueueId_NotFound()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/history/99999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
            // Purge records "older than 0 days" = purge all
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

        [TestMethod]
        public async Task PurgeHistory_FutureDays_Removes_Nothing()
        {
            // Purge records older than 365 days - none should qualify since they were just created
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{_queueId}/history?olderThanDays=365");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<DeleteAllResponse>();
            result.Deleted.Should().Be(0);

            // Verify count is unchanged
            var countResponse = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/history/count");
            var count = await countResponse.Content.ReadFromJsonAsync<long>();
            count.Should().BeGreaterThanOrEqualTo(MessageCount);
        }
    }
}
