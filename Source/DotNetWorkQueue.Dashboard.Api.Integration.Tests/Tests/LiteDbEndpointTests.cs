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
using DotNetWorkQueue.Transport.LiteDb.Basic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    [TestClass]
    public class LiteDbEndpointTests
    {
        private DashboardTestServer _server;
        private TransportFixture<LiteDbMessageQueueInit, LiteDbMessageQueueCreation> _fixture;
        private Guid _connectionId;
        private Guid _queueId;
        private string _queueName;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            _queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.LiteDbMemory;

            _fixture = new TransportFixture<LiteDbMessageQueueInit, LiteDbMessageQueueCreation>(
                _queueName, connStr,
                options =>
                {
                    options.Options.EnableStatusTable = true;
                });

            _fixture.SendMessages<FakeMessage>(5);

            _server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<LiteDbMessageQueueInit>(connStr,
                    serviceRegister => serviceRegister.RegisterNonScopedSingleton(_fixture.Scope),
                    conn => conn.AddQueue(_queueName));
            });

            var connections = await _server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            connections.Should().HaveCount(1);
            _connectionId = connections[0].Id;

            var queues = await _server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{_connectionId}/queues");
            queues.Should().HaveCount(1);
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
            connections.Should().HaveCount(1);
            connections[0].QueueCount.Should().Be(1);
        }

        [TestMethod]
        public async Task Queues_ReturnsOne()
        {
            var queues = await _server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{_connectionId}/queues");
            queues.Should().HaveCount(1);
            queues[0].QueueName.Should().Be(_queueName);
        }

        [TestMethod]
        public async Task Queues_InvalidConnection_Returns404()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/connections/{Guid.NewGuid()}/queues");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // === Status & Features ===

        [TestMethod]
        public async Task Status_AllWaiting()
        {
            var status = await _server.Client.GetFromJsonAsync<QueueStatusResponse>(
                $"api/v1/dashboard/queues/{_queueId}/status");
            status.Waiting.Should().Be(5);
            status.Processing.Should().Be(0);
            status.Total.Should().Be(5);
        }

        [TestMethod]
        public async Task Features_ReturnsExpected()
        {
            var features = await _server.Client.GetFromJsonAsync<QueueFeaturesResponse>(
                $"api/v1/dashboard/queues/{_queueId}/features");
            features.EnableStatusTable.Should().BeTrue();
        }

        // === Message Listing ===

        [TestMethod]
        public async Task Messages_ReturnsAll()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?pageSize=100");
            paged.Items.Should().HaveCount(5);
        }

        [TestMethod]
        public async Task Messages_Pagination()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?pageSize=2&pageIndex=0");
            paged.Items.Should().HaveCount(2);
            paged.TotalCount.Should().Be(5);
        }

        [TestMethod]
        public async Task Messages_WaitingFilter()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?status=0&pageSize=100");
            paged.Items.Should().HaveCount(5);
        }

        [TestMethod]
        public async Task MessageCount_NoFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/count");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var count = await response.Content.ReadFromJsonAsync<long>();
            count.Should().Be(5);
        }

        [TestMethod]
        public async Task MessageCount_WaitingFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/count?status=0");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var count = await response.Content.ReadFromJsonAsync<long>();
            count.Should().Be(5);
        }

        [TestMethod]
        public async Task MessageCount_InvalidStatus_Returns400()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/count?status=99");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
            detail.QueueId.Should().Be(messageId);
        }

        [TestMethod]
        public async Task MessageDetail_NotFound()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/999999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task MessageBody_HasContent()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?pageSize=1");
            var messageId = paged.Items[0].QueueId;

            var body = await _server.Client.GetFromJsonAsync<MessageBodyResponse>(
                $"api/v1/dashboard/queues/{_queueId}/messages/{messageId}/body");
            body.Body.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task MessageHeaders_HasContent()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?pageSize=1");
            var messageId = paged.Items[0].QueueId;

            var headers = await _server.Client.GetFromJsonAsync<MessageHeadersResponse>(
                $"api/v1/dashboard/queues/{_queueId}/messages/{messageId}/headers");
            headers.Headers.Should().NotBeNull();
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
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var countResponse = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/count");
            var count = await countResponse.Content.ReadFromJsonAsync<long>();
            count.Should().Be(4);
        }

        [TestMethod]
        public async Task DeleteMessage_NotFound()
        {
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/999999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // === LiteDb-specific ===

        [TestMethod]
        public async Task Errors_WhenEmpty()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<ErrorMessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/errors");
            paged.Items.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Jobs_ReturnsEmpty()
        {
            var jobs = await _server.Client.GetFromJsonAsync<List<JobResponse>>(
                $"api/v1/dashboard/connections/{_connectionId}/jobs");
            jobs.Should().BeEmpty();
        }

        [TestMethod]
        public async Task StaleMessages_WhenEmpty()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages/stale");
            paged.Items.Should().BeEmpty();
        }

        [TestMethod]
        public async Task DeleteAllErrors_WhenEmpty()
        {
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{_queueId}/errors");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<DeleteAllResponse>();
            result.Deleted.Should().Be(0);
        }

        [TestMethod]
        public async Task RequeueError_NotFound()
        {
            var response = await _server.Client.PostAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/999999/requeue", null);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
