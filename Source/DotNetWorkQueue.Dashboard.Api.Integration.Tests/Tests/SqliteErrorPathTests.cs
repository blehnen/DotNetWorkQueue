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
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Features_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/features");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Configuration_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/configuration");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Maintenance_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/maintenance");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Messages_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/messages?pageSize=10");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task MessageCount_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/messages/count");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Errors_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/errors");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task History_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/history");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        // === DashboardExceptionFilter: InvalidOperationException for connections ===

        [TestMethod]
        public async Task Queues_NonexistentConnection_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/connections/{Guid.NewGuid()}/queues");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Jobs_NonexistentConnection_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/connections/{Guid.NewGuid()}/jobs");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        // === Message detail/body/headers for nonexistent messages ===

        [TestMethod]
        public async Task MessageDetail_NonexistentId_Returns404()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/99999999");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task MessageBody_NonexistentId_Returns404()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/99999999/body");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task MessageHeaders_NonexistentId_Returns404()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/99999999/headers");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        // === Requeue/reset for nonexistent messages ===

        [TestMethod]
        public async Task RequeueError_NonexistentMessage_Returns404()
        {
            var response = await _server.Client.PostAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/99999999/requeue", null);
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task ResetStale_NonexistentMessage_Returns404()
        {
            var response = await _server.Client.PostAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/99999999/reset", null);
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        // === Delete nonexistent message ===

        [TestMethod]
        public async Task DeleteMessage_NonexistentId_Returns404()
        {
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/99999999");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        // === Invalid status parameter ===

        [TestMethod]
        public async Task Messages_InvalidStatus_Returns400()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages?status=99");
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task MessageCount_InvalidStatus_Returns400()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/count?status=99");
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Messages_LargeNegativeStatus_Returns400()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages?status=-99");
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
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
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
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
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        // === Stale messages endpoint on nonexistent queue returns 404 ===

        [TestMethod]
        public async Task StaleMessages_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/messages/stale");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        // === Delete all errors on nonexistent queue returns 404 ===

        [TestMethod]
        public async Task DeleteAllErrors_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/errors");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        // === History endpoints on nonexistent queue ===

        [TestMethod]
        public async Task HistoryCount_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/history/count");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task HistoryByMessageId_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/history/99999");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task PurgeHistory_NonexistentQueue_Returns404_ViaExceptionFilter()
        {
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/history");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        // === Exception filter returns JSON error body ===

        [TestMethod]
        public async Task ExceptionFilter_ReturnsJsonErrorBody()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/status");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(body));

            // The exception filter returns { "error": "..." }
            var doc = JsonDocument.Parse(body);
            Assert.IsTrue(doc.RootElement.TryGetProperty("error", out var errorProp));
            StringAssert.Contains(errorProp.GetString(), "An internal error occurred");
        }
    }
}
