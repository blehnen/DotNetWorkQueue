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
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers;
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Transport.SQLite.Basic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    /// <summary>
    /// Tests for error/exception paths in the Dashboard API, including the DashboardExceptionFilter
    /// behavior and invalid request handling.
    /// </summary>
    [TestClass]
    public class SqliteErrorPathTests
    {
        private DashboardTestServer _server;
        private TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation> _fixture;
        private Guid _connectionId;
        private Guid _queueId;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.CreateSqliteInMemory(queueName);

            _fixture = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                    options.Options.EnableHeartBeat = true;
                });

            _fixture.SendMessages<FakeMessage>(3);

            _server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var connections = await _server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            _connectionId = connections[0].Id;

            var queues = await _server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{_connectionId}/queues");
            _queueId = queues[0].Id;
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (_server != null) await _server.DisposeAsync();
            _fixture?.Dispose();
        }

        // === DashboardExceptionFilter: InvalidOperationException -> 404 ===
        // The exception filter catches InvalidOperationException from DashboardService
        // when a queue ID is not found.

        [TestMethod]
        public async Task Status_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/status");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task Features_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/features");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task Configuration_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/configuration");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task Maintenance_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/maintenance");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task Messages_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/messages?pageSize=10");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task MessageCount_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/messages/count");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task Errors_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/errors");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task History_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/history");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // === DashboardExceptionFilter: InvalidOperationException for connections ===

        [TestMethod]
        public async Task Queues_NonexistentConnection_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/connections/{Guid.NewGuid()}/queues");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task Jobs_NonexistentConnection_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/connections/{Guid.NewGuid()}/jobs");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // === Message detail/body/headers for nonexistent messages ===

        [TestMethod]
        public async Task MessageDetail_NonexistentId_Returns404()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/99999999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task MessageBody_NonexistentId_Returns404()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/99999999/body");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task MessageHeaders_NonexistentId_Returns404()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/99999999/headers");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // === Requeue/reset for nonexistent messages ===

        [TestMethod]
        public async Task RequeueError_NonexistentMessage_Returns404()
        {
            var response = await _server.Client.PostAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/99999999/requeue", null);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task ResetStale_NonexistentMessage_Returns404()
        {
            var response = await _server.Client.PostAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/99999999/reset", null);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // === Delete nonexistent message ===

        [TestMethod]
        public async Task DeleteMessage_NonexistentId_Returns404()
        {
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/99999999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // === Invalid status parameter ===

        [TestMethod]
        public async Task Messages_InvalidStatus_Returns400()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages?status=99");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task MessageCount_InvalidStatus_Returns400()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/count?status=99");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task Messages_LargeNegativeStatus_Returns400()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages?status=-99");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // === Edit body with null body returns 400 ===

        [TestMethod]
        public async Task EditBody_NullBody_Returns400()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?pageSize=1");
            var messageId = paged.Items[0].QueueId;

            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await _server.Client.PutAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/{messageId}/body", content);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // === Edit body for nonexistent message returns 404 ===

        [TestMethod]
        public async Task EditBody_NonexistentMessage_Returns404()
        {
            var content = new StringContent(
                "{\"Body\":\"{\\\"Message\\\":\\\"test\\\",\\\"Value\\\":1}\"}",
                Encoding.UTF8, "application/json");
            var response = await _server.Client.PutAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/99999999/body", content);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // === Stale messages endpoint on nonexistent queue returns 404 ===

        [TestMethod]
        public async Task StaleMessages_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/messages/stale");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // === Delete all errors on nonexistent queue returns 404 ===

        [TestMethod]
        public async Task DeleteAllErrors_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/errors");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // === History endpoints on nonexistent queue ===

        [TestMethod]
        public async Task HistoryCount_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/history/count");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task HistoryByMessageId_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/history/99999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task PurgeHistory_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/history");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // === Exception filter returns JSON error body ===

        [TestMethod]
        public async Task ExceptionFilter_ReturnsJsonErrorBody()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/status");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().NotBeNullOrEmpty();

            // The exception filter returns { "error": "..." }
            var doc = JsonDocument.Parse(body);
            doc.RootElement.TryGetProperty("error", out var errorProp).Should().BeTrue();
            errorProp.GetString().Should().Contain("was not found");
        }
    }
}
