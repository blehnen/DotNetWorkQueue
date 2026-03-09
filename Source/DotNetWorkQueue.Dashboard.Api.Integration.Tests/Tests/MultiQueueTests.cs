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
    public class MultiQueueTests
    {
        private DashboardTestServer _server;
        private TransportFixture<MemoryDashboardInit, MessageQueueCreation> _fixture1;
        private TransportFixture<MemoryDashboardInit, MessageQueueCreation> _fixture2;
        private Guid _connectionId;
        private Guid _queueId1;
        private Guid _queueId2;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            var queueName1 = QueueNameGenerator.Create();
            var queueName2 = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.Memory;

            _fixture1 = new TransportFixture<MemoryDashboardInit, MessageQueueCreation>(
                queueName1, connStr);
            _fixture2 = new TransportFixture<MemoryDashboardInit, MessageQueueCreation>(
                queueName2, connStr);

            _fixture1.SendMessages<FakeMessage>(3);
            _fixture2.SendMessages<FakeMessage>(2);

            _server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<MemoryDashboardInit>(connStr,
                    serviceRegister =>
                    {
                        serviceRegister.RegisterNonScopedSingleton(_fixture1.Scope);
                        serviceRegister.RegisterNonScopedSingleton(_fixture2.Scope);
                    },
                    conn =>
                    {
                        conn.AddQueue(queueName1);
                        conn.AddQueue(queueName2);
                    });
            });

            var connections = await _server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            connections.Should().HaveCount(1);
            _connectionId = connections[0].Id;

            var queues = await _server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{_connectionId}/queues");
            queues.Should().HaveCount(2);

            // Assign queue IDs by matching queue names
            foreach (var q in queues)
            {
                if (q.QueueName == queueName1)
                    _queueId1 = q.Id;
                else if (q.QueueName == queueName2)
                    _queueId2 = q.Id;
            }
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (_server != null) await _server.DisposeAsync();
            _fixture1?.Dispose();
            _fixture2?.Dispose();
        }

        [TestMethod]
        public async Task GetConnections_ReturnsBoth()
        {
            var connections = await _server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            connections.Should().HaveCount(1);
            connections[0].QueueCount.Should().Be(2);
        }

        [TestMethod]
        public async Task Messages_Isolated_PerQueue()
        {
            var paged1 = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId1}/messages?pageSize=100");
            paged1.Items.Should().HaveCount(3);

            var paged2 = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId2}/messages?pageSize=100");
            paged2.Items.Should().HaveCount(2);
        }

        [TestMethod]
        public async Task Status_Isolated_PerQueue()
        {
            var status1 = await _server.Client.GetFromJsonAsync<QueueStatusResponse>(
                $"api/v1/dashboard/queues/{_queueId1}/status");
            status1.Waiting.Should().Be(3);

            var status2 = await _server.Client.GetFromJsonAsync<QueueStatusResponse>(
                $"api/v1/dashboard/queues/{_queueId2}/status");
            status2.Waiting.Should().Be(2);
        }

        [TestMethod]
        public async Task Queue_NotFound_Returns404()
        {
            var response = await _server.Client.GetAsync(
                $"api/v1/dashboard/queues/{Guid.NewGuid()}/status");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
