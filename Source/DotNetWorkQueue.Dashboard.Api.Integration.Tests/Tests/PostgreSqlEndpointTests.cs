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
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers;
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    [TestClass]
    public class PostgreSqlEndpointTests
    {
        private DashboardTestServer _server;
        private TransportFixture<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation> _fixture;
        private Guid _connectionId;
        private Guid _queueId;
        private string _queueName;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            _queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.PostgreSql;

            _fixture = new TransportFixture<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(
                _queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                    options.Options.EnableHeartBeat = true;
                });

            _fixture.SendMessages<FakeMessage>(5);

            _server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<PostgreSqlMessageQueueInit>(connStr,
                    conn => conn.AddQueue(_queueName));
            });

            var connections = await _server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            Assert.AreEqual(1, connections.Count);
            _connectionId = connections[0].Id;

            var queues = await _server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{_connectionId}/queues");
            Assert.AreEqual(1, queues.Count);
            _queueId = queues[0].Id;
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (_server != null) await _server.DisposeAsync();
            _fixture?.Dispose();
        }

        // === Connections & Discovery ===

        [TestMethod]
        public async Task Connections_ReturnsOne()
        {
            var connections = await _server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            Assert.AreEqual(1, connections.Count);
            Assert.AreEqual(1, connections[0].QueueCount);
        }

        [TestMethod]
        public async Task Queues_ReturnsOne()
        {
            var queues = await _server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{_connectionId}/queues");
            Assert.AreEqual(1, queues.Count);
            Assert.AreEqual(_queueName, queues[0].QueueName);
        }

        [TestMethod]
        public async Task Queues_InvalidConnection_Returns404()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/connections/{Guid.NewGuid()}/queues");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        // === Status & Features ===

        [TestMethod]
        public async Task Status_AllWaiting()
        {
            var status = await _server.Client.GetFromJsonAsync<QueueStatusResponse>(
                $"api/v1/dashboard/queues/{_queueId}/status");
            Assert.AreEqual(5, status.Waiting);
            Assert.AreEqual(0, status.Processing);
            Assert.AreEqual(5, status.Total);
        }

        [TestMethod]
        public async Task Features_ReturnsExpected()
        {
            var features = await _server.Client.GetFromJsonAsync<QueueFeaturesResponse>(
                $"api/v1/dashboard/queues/{_queueId}/features");
            Assert.IsTrue(features.EnableStatus);
            Assert.IsTrue(features.EnableStatusTable);
            Assert.IsTrue(features.EnableHeartBeat);
        }

        // === Message Listing ===

        [TestMethod]
        public async Task Messages_ReturnsAll()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?pageSize=100");
            Assert.AreEqual(5, paged.Items.Count);
        }

        [TestMethod]
        public async Task Messages_Pagination()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?pageSize=2&pageIndex=0");
            Assert.AreEqual(2, paged.Items.Count);
            Assert.AreEqual(5, paged.TotalCount);
        }

        [TestMethod]
        public async Task Messages_WaitingFilter()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?status=0&pageSize=100");
            Assert.AreEqual(5, paged.Items.Count);
        }

        [TestMethod]
        public async Task MessageCount_NoFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/count");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var count = await response.Content.ReadFromJsonAsync<long>();
            Assert.AreEqual(5, count);
        }

        [TestMethod]
        public async Task MessageCount_WaitingFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/count?status=0");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var count = await response.Content.ReadFromJsonAsync<long>();
            Assert.AreEqual(5, count);
        }

        [TestMethod]
        public async Task MessageCount_InvalidStatus_Returns400()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/count?status=99");
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // === Message Detail/Body/Headers ===

        [TestMethod]
        public async Task MessageDetail_Exists()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?pageSize=1");
            var messageId = paged.Items[0].QueueId;

            var detail = await _server.Client.GetFromJsonAsync<MessageResponse>(
                $"api/v1/dashboard/queues/{_queueId}/messages/{messageId}");
            Assert.AreEqual(messageId, detail.QueueId);
        }

        [TestMethod]
        public async Task MessageDetail_NotFound()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/99999999");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task MessageBody_HasContent()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?pageSize=1");
            var messageId = paged.Items[0].QueueId;

            var body = await _server.Client.GetFromJsonAsync<MessageBodyResponse>(
                $"api/v1/dashboard/queues/{_queueId}/messages/{messageId}/body");
            Assert.IsFalse(string.IsNullOrEmpty(body.Body));
        }

        [TestMethod]
        public async Task MessageHeaders_HasContent()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?pageSize=1");
            var messageId = paged.Items[0].QueueId;

            var headers = await _server.Client.GetFromJsonAsync<MessageHeadersResponse>(
                $"api/v1/dashboard/queues/{_queueId}/messages/{messageId}/headers");
            Assert.IsNotNull(headers.Headers);
        }

        // === Delete ===

        [TestMethod]
        public async Task DeleteMessage_Exists()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?pageSize=1");
            var messageId = paged.Items[0].QueueId;

            var deleteResponse = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/{messageId}");
            Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            var countResponse = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/count");
            var count = await countResponse.Content.ReadFromJsonAsync<long>();
            Assert.AreEqual(4, count);
        }

        [TestMethod]
        public async Task DeleteMessage_NotFound()
        {
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/99999999");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        // === Relational-specific ===

        [TestMethod]
        public async Task Configuration_ReturnsJson()
        {
            var config = await _server.Client.GetFromJsonAsync<ConfigurationResponse>(
                $"api/v1/dashboard/queues/{_queueId}/configuration");
            Assert.IsFalse(string.IsNullOrEmpty(config.ConfigurationJson));
        }

        [TestMethod]
        public async Task Jobs_Returns200()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/connections/{_connectionId}/jobs");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var jobs = await response.Content.ReadFromJsonAsync<List<JobResponse>>();
            Assert.IsNotNull(jobs);
        }

        [TestMethod]
        public async Task Errors_WhenEmpty()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<ErrorMessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/errors");
            Assert.AreEqual(0, paged.Items.Count);
        }

        [TestMethod]
        public async Task StaleMessages_WhenEmpty()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages/stale");
            Assert.AreEqual(0, paged.Items.Count);
        }

        [TestMethod]
        public async Task DeleteAllErrors_WhenEmpty()
        {
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{_queueId}/errors");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DeleteAllResponse>();
            Assert.AreEqual(0, result.Deleted);
        }

        [TestMethod]
        public async Task RequeueError_NotFound()
        {
            var response = await _server.Client.PostAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/99999999/requeue", null);
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
