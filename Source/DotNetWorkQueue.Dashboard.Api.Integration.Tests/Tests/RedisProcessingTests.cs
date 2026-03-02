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
using DotNetWorkQueue.Transport.Redis.Basic;
using FluentAssertions;
using Xunit;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    public class RedisProcessingTests : IAsyncLifetime
    {
        private DashboardTestServer _server;
        private TransportFixture<RedisQueueInit, RedisQueueCreation> _fixture;
        private ConsumerStateHelper<RedisQueueInit> _consumerHelper;
        private Guid _queueId;

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

            _consumerHelper = new ConsumerStateHelper<RedisQueueInit>();
            _consumerHelper.StartBlockingConsumer(_fixture.QueueConnection, _fixture.Scope);
            await DashboardPollingHelper.WaitForStatusAsync(_server.Client, _queueId,
                s => s.Processing >= 1);
        }

        public async Task DisposeAsync()
        {
            _consumerHelper?.Dispose();
            if (_server != null) await _server.DisposeAsync();
            _fixture?.Dispose();
        }

        [Fact]
        public async Task Status_WithProcessing()
        {
            var status = await _server.Client.GetFromJsonAsync<QueueStatusResponse>(
                $"api/v1/dashboard/queues/{_queueId}/status");
            status.Processing.Should().BeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task Messages_ProcessingFilter()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?status=1&pageSize=100");
            paged.Items.Should().NotBeEmpty();
        }

        [Fact]
        public async Task DeleteMessage_Processing()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?status=1&pageSize=1");
            paged.Items.Should().NotBeEmpty();
            var messageId = paged.Items[0].QueueId;

            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/{messageId}");
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }
}
