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
using Xunit;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    public class EmptyQueueTests : IAsyncLifetime
    {
        private DashboardTestServer _server;
        private TransportFixture<MemoryDashboardInit, MessageQueueCreation> _fixture;
        private Guid _queueId;

        public async Task InitializeAsync()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.Memory;

            _fixture = new TransportFixture<MemoryDashboardInit, MessageQueueCreation>(
                queueName, connStr);

            // No messages sent — empty queue

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

        public async Task DisposeAsync()
        {
            if (_server != null) await _server.DisposeAsync();
            _fixture?.Dispose();
        }

        [Fact]
        public async Task Status_EmptyQueue()
        {
            var status = await _server.Client.GetFromJsonAsync<QueueStatusResponse>(
                $"api/v1/dashboard/queues/{_queueId}/status");
            status.Waiting.Should().Be(0);
            status.Processing.Should().Be(0);
            status.Total.Should().Be(0);
        }

        [Fact]
        public async Task Messages_EmptyQueue()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?pageSize=100");
            paged.Items.Should().BeEmpty();
            paged.TotalCount.Should().Be(0);
        }
    }
}
