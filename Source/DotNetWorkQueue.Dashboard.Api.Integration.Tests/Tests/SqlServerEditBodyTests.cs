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
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers;
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    [TestClass]
    public class SqlServerEditBodyTests
    {
        private DashboardTestServer _server;
        private TransportFixture<SqlServerMessageQueueInit, SqlServerMessageQueueCreation> _fixture;
        private ConsumerStateHelper<SqlServerMessageQueueInit> _processingHelper;
        private Guid _queueId;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.SqlServer;

            _fixture = new TransportFixture<SqlServerMessageQueueInit, SqlServerMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                    options.Options.EnableHeartBeat = true;
                });

            _fixture.SendMessages<FakeMessage>(2);

            _server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<SqlServerMessageQueueInit>(connStr,
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
            _processingHelper?.Dispose();
            if (_server != null) await _server.DisposeAsync();
            _fixture?.Dispose();
        }

        [TestMethod]
        public async Task EditBody_Success()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?pageSize=1");
            var messageId = paged.Items[0].QueueId;

            var content = new StringContent(
                "{\"Body\":\"{\\\"Message\\\":\\\"edited\\\",\\\"Value\\\":99}\"}",
                Encoding.UTF8, "application/json");
            var response = await _server.Client.PutAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/{messageId}/body", content);
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [TestMethod]
        public async Task EditBody_NotFound()
        {
            var content = new StringContent(
                "{\"Body\":\"{\\\"Message\\\":\\\"edited\\\",\\\"Value\\\":99}\"}",
                Encoding.UTF8, "application/json");
            var response = await _server.Client.PutAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/99999999/body", content);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task EditBody_InvalidJson()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?pageSize=1");
            var messageId = paged.Items[0].QueueId;

            var content = new StringContent(
                "{\"Body\":\"not json{\"}",
                Encoding.UTF8, "application/json");
            var response = await _server.Client.PutAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/{messageId}/body", content);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task EditBody_ProcessingMessage()
        {
            _processingHelper = new ConsumerStateHelper<SqlServerMessageQueueInit>();
            _processingHelper.StartBlockingConsumer(_fixture.QueueConnection, _fixture.Scope);
            await DashboardPollingHelper.WaitForStatusAsync(_server.Client, _queueId,
                s => s.Processing >= 1);

            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?status=1&pageSize=1");
            paged.Items.Should().NotBeEmpty();
            var messageId = paged.Items[0].QueueId;

            var content = new StringContent(
                "{\"Body\":\"{\\\"Message\\\":\\\"edited\\\",\\\"Value\\\":99}\"}",
                Encoding.UTF8, "application/json");
            var response = await _server.Client.PutAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/{messageId}/body", content);
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
    }
}
