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
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    [TestClass]
    public class MemoryStatefulTests
    {
        private DashboardTestServer _server;
        private TransportFixture<MemoryDashboardInit, MessageQueueCreation> _fixture;
        private ConsumerStateHelper<MemoryDashboardInit> _consumerHelper;
        private Guid _queueId;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.Memory;

            _fixture = new TransportFixture<MemoryDashboardInit, MessageQueueCreation>(
                queueName, connStr);

            _fixture.SendMessages<FakeMessage>(5);

            _server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<MemoryDashboardInit>(connStr,
                    serviceRegister => serviceRegister.RegisterNonScopedSingleton(_fixture.Scope),
                    conn => conn.AddQueue(queueName));
            });

            var connections = await _server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            connections.Should().HaveCount(1);

            var queues = await _server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            queues.Should().HaveCount(1);
            _queueId = queues[0].Id;

            // Start blocking consumer with 2 workers — each picks up 1 message and blocks
            _consumerHelper = new ConsumerStateHelper<MemoryDashboardInit>();
            _consumerHelper.StartBlockingConsumer(_fixture.QueueConnection, _fixture.Scope, workerCount: 2);
            await DashboardPollingHelper.WaitForStatusAsync(_server.Client, _queueId,
                s => s.Processing >= 2);
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            _consumerHelper?.Dispose();
            if (_server != null) await _server.DisposeAsync();
            _fixture?.Dispose();
        }

        [TestMethod]
        public async Task Status_WithProcessing()
        {
            var status = await _server.Client.GetFromJsonAsync<QueueStatusResponse>(
                $"api/v1/dashboard/queues/{_queueId}/status");
            status.Waiting.Should().Be(3);
            status.Processing.Should().Be(2);
            status.Total.Should().Be(5);
        }

        [TestMethod]
        public async Task MessageCount_ProcessingFilter()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/count?status=1");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var count = await response.Content.ReadFromJsonAsync<long>();
            count.Should().Be(2);
        }

        [TestMethod]
        public async Task Messages_ProcessingFilter()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?status=1&pageSize=100");
            paged.Items.Should().HaveCount(2);
        }

        [TestMethod]
        public async Task Messages_NoFilter_AllStates()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?pageSize=100");
            paged.Items.Should().HaveCount(5);
        }

        [TestMethod]
        public async Task MessageDetail_ProcessingMessage()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?status=1&pageSize=1");
            paged.Items.Should().NotBeEmpty();
            var messageId = paged.Items[0].QueueId;

            var detail = await _server.Client.GetFromJsonAsync<MessageResponse>(
                $"api/v1/dashboard/queues/{_queueId}/messages/{messageId}");
            detail.QueueId.Should().Be(messageId);
        }

        [TestMethod]
        public async Task MessageBody_ProcessingMessage()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?status=1&pageSize=1");
            paged.Items.Should().NotBeEmpty();
            var messageId = paged.Items[0].QueueId;

            var body = await _server.Client.GetFromJsonAsync<MessageBodyResponse>(
                $"api/v1/dashboard/queues/{_queueId}/messages/{messageId}/body");
            body.Body.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task DeleteMessage_Processing()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?status=1&pageSize=1");
            paged.Items.Should().NotBeEmpty();
            var messageId = paged.Items[0].QueueId;

            var deleteResponse = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/{messageId}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var status = await _server.Client.GetFromJsonAsync<QueueStatusResponse>(
                $"api/v1/dashboard/queues/{_queueId}/status");
            status.Total.Should().Be(4);
        }
    }
}
