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
using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetWorkQueue.Tests.Shared;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    /// <summary>
    /// Tests for the history API endpoints when history is NOT enabled on a Redis transport.
    /// The NoOp handlers should return empty results.
    /// </summary>
    [TestClass]
    public class RedisHistoryDisabledTests
    {
        private DashboardTestServer _server;
        private TransportFixture<RedisQueueInit, RedisQueueCreation> _fixture;
        private Guid _queueId;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.Redis;

            _fixture = new TransportFixture<RedisQueueInit, RedisQueueCreation>(
                queueName, connStr);

            _fixture.SendMessages<FakeMessage>(3);

            _server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<RedisQueueInit>(connStr,
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

            Assert.IsNotNull(result);
            Assert.IsEmpty(result.Items);
        }

        [TestMethod]
        public async Task HistoryCount_Returns_Zero_When_Not_Enabled()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/history/count");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var count = await response.Content.ReadFromJsonAsync<long>();
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public async Task HistoryByMessageId_Returns_NotFound_When_Not_Enabled()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/history/nonexistent-id-12345");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task PurgeHistory_Returns_Zero_When_Not_Enabled()
        {
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{_queueId}/history");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DeleteAllResponse>();
            Assert.AreEqual(0, result.Deleted);
        }
    }

    /// <summary>
    /// Tests for the history API endpoints when history IS enabled on a Redis transport.
    /// Messages are sent and consumed to completion so that history records are populated.
    /// </summary>
    [TestClass]
    public class RedisHistoryEnabledTests
    {
        private const int MessageCount = 5;
        private DashboardTestServer _server;
        private string _queueName;
        private QueueCreationContainer<RedisQueueInit> _creationContainer;
        private RedisQueueCreation _creation;
        private ICreationScope _scope;
        private Guid _queueId;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            _queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.Redis;
            var queueConnection = new QueueConnection(_queueName, connStr);

            // Create queue with history enabled
            _creationContainer = new QueueCreationContainer<RedisQueueInit>();
            _creation = _creationContainer.GetQueueCreation<RedisQueueCreation>(queueConnection);
            _creation.Options.EnableHistory = true;
            var createResult = _creation.CreateQueue();
            Assert.IsTrue(createResult.Success, createResult.ErrorMessage);
            _scope = _creation.Scope;

            // Send and consume messages to completion (generates history records)
            using (var queueContainer = new QueueContainer<RedisQueueInit>())
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

                    waitHandle.Wait(TimeSpan.FromSeconds(60)); // Redis needs longer timeout than Memory due to network round-trips under CI load
                }

                Assert.AreEqual(MessageCount, processedCount, "Not all messages were processed");
            }

            // Start Dashboard server
            _server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<RedisQueueInit>(connStr,
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
            _scope?.Dispose();
            _creation?.Dispose();
            _creationContainer?.Dispose();
        }

        [TestMethod]
        public async Task History_Returns_Records_When_Enabled()
        {
            var result = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history?pageSize=100");

            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result.Items);
            Assert.IsGreaterThanOrEqualTo(MessageCount, result.Items.Count);
        }

        [TestMethod]
        public async Task History_Pagination_Page0()
        {
            var result = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history?pageIndex=0&pageSize=2");

            Assert.IsNotNull(result);
            Assert.HasCount(2, result.Items);
            Assert.IsGreaterThanOrEqualTo(MessageCount, result.TotalCount);
            Assert.AreEqual(0, result.PageIndex);
            Assert.AreEqual(2, result.PageSize);
        }

        [TestMethod]
        public async Task History_Pagination_Page1()
        {
            var result = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history?pageIndex=1&pageSize=2");

            Assert.IsNotNull(result);
            Assert.HasCount(2, result.Items);
            Assert.AreEqual(1, result.PageIndex);
        }

        [TestMethod]
        public async Task History_Pagination_BeyondLast_Returns_Empty()
        {
            var result = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history?pageIndex=100&pageSize=25");

            Assert.IsNotNull(result);
            Assert.IsEmpty(result.Items);
            Assert.IsGreaterThanOrEqualTo(MessageCount, result.TotalCount);
        }

        [TestMethod]
        public async Task History_Filtered_By_Complete_Status()
        {
            // Status 2 = Complete
            var result = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history?status=2&pageSize=100");

            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result.Items);
            AssertHelper.AllSatisfy(result.Items, item => Assert.AreEqual(2, item.Status));
        }

        [TestMethod]
        public async Task History_Filtered_By_Error_Status_Returns_Empty()
        {
            // Status 3 = Error -- no errors in this test
            var result = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history?status=3&pageSize=100");

            Assert.IsNotNull(result);
            Assert.IsEmpty(result.Items);
        }

        [TestMethod]
        public async Task History_Filtered_By_Processing_Status_Returns_Empty()
        {
            // Status 1 = Processing -- all messages have completed
            var result = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history?status=1&pageSize=100");

            Assert.IsNotNull(result);
            Assert.IsEmpty(result.Items);
        }

        [TestMethod]
        public async Task HistoryCount_NoFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/history/count");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var count = await response.Content.ReadFromJsonAsync<long>();
            Assert.IsGreaterThanOrEqualTo(MessageCount, count);
        }

        [TestMethod]
        public async Task HistoryCount_WithCompleteStatusFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/history/count?status=2");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var count = await response.Content.ReadFromJsonAsync<long>();
            Assert.IsGreaterThanOrEqualTo(MessageCount, count);
        }

        [TestMethod]
        public async Task HistoryCount_WithErrorStatusFilter_Returns_Zero()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/history/count?status=3");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var count = await response.Content.ReadFromJsonAsync<long>();
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public async Task HistoryByQueueId_Returns_Record()
        {
            var history = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history?pageSize=1");
            Assert.IsNotEmpty(history.Items);
            var recordQueueId = history.Items[0].QueueId;

            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/history/{recordQueueId}");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var record = await response.Content.ReadFromJsonAsync<HistoryResponse>();
            Assert.IsNotNull(record);
            Assert.AreEqual(recordQueueId, record.QueueId);
            Assert.AreEqual(2, record.Status); // Complete
        }

        [TestMethod]
        public async Task HistoryByQueueId_NotFound()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/history/nonexistent-id-12345");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task History_Records_Have_Expected_Fields()
        {
            var result = await _server.Client.GetFromJsonAsync<PagedResponse<HistoryResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/history?pageSize=1");

            Assert.IsNotEmpty(result.Items);
            var record = result.Items[0];

            Assert.IsFalse(string.IsNullOrEmpty(record.QueueId));
            Assert.AreEqual(2, record.Status); // Complete
            Assert.IsGreaterThan(DateTime.MinValue, record.EnqueuedUtc);
        }

        [TestMethod]
        public async Task PurgeHistory_WithDateFilter_Removes_Records()
        {
            // Purge records "older than 0 days" = purge all
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{_queueId}/history?olderThanDays=0");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DeleteAllResponse>();
            Assert.IsGreaterThanOrEqualTo(MessageCount, result.Deleted);

            // Verify count is now 0
            var countResponse = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/history/count");
            var count = await countResponse.Content.ReadFromJsonAsync<long>();
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public async Task PurgeHistory_FutureDays_Removes_Nothing()
        {
            // Purge records older than 365 days - none should qualify since they were just created
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{_queueId}/history?olderThanDays=365");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DeleteAllResponse>();
            Assert.AreEqual(0, result.Deleted);

            // Verify count is unchanged
            var countResponse = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/history/count");
            var count = await countResponse.Content.ReadFromJsonAsync<long>();
            Assert.IsGreaterThanOrEqualTo(MessageCount, count);
        }
    }
}
