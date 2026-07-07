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
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers;
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    [TestClass]
    public class MultiSourceTests
    {
        private DashboardTestServer _server1;
        private DashboardTestServer _server2;
        private TransportFixture<MemoryDashboardInit, MessageQueueCreation> _fixture1;
        private TransportFixture<MemoryDashboardInit, MessageQueueCreation> _fixture2;
        private string _queueName1;
        private string _queueName2;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            _queueName1 = QueueNameGenerator.Create();
            _queueName2 = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.Memory;

            _fixture1 = new TransportFixture<MemoryDashboardInit, MessageQueueCreation>(
                _queueName1, connStr);
            _fixture2 = new TransportFixture<MemoryDashboardInit, MessageQueueCreation>(
                _queueName2, connStr);

            _fixture1.SendMessages<FakeMessage>(3);
            _fixture2.SendMessages<FakeMessage>(2);

            _server1 = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<MemoryDashboardInit>(connStr,
                    serviceRegister => serviceRegister.RegisterNonScopedSingleton(_fixture1.Scope),
                    conn => conn.AddQueue(_queueName1));
            });

            _server2 = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<MemoryDashboardInit>(connStr,
                    serviceRegister => serviceRegister.RegisterNonScopedSingleton(_fixture2.Scope),
                    conn => conn.AddQueue(_queueName2));
            });
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (_server1 != null) await _server1.DisposeAsync();
            if (_server2 != null) await _server2.DisposeAsync();
            _fixture1?.Dispose();
            _fixture2?.Dispose();
        }

        [TestMethod]
        public async Task Source1_ReturnsOwnConnections()
        {
            var connections = await _server1.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            Assert.HasCount(1, connections);
            Assert.AreEqual(1, connections[0].QueueCount);
        }

        [TestMethod]
        public async Task Source2_ReturnsOwnConnections()
        {
            var connections = await _server2.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            Assert.HasCount(1, connections);
            Assert.AreEqual(1, connections[0].QueueCount);
        }

        [TestMethod]
        public async Task Sources_HaveIndependentConnections()
        {
            var connections1 = await _server1.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var connections2 = await _server2.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");

            Assert.HasCount(1, connections1);
            Assert.HasCount(1, connections2);

            var ids1 = connections1.Select(c => c.Id).ToHashSet();
            var ids2 = connections2.Select(c => c.Id).ToHashSet();
            Assert.IsFalse(ids1.Overlaps(ids2));
        }

        [TestMethod]
        public async Task Source1_Messages_MatchSendCount()
        {
            var connections = await _server1.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var connectionId = connections[0].Id;

            var queues = await _server1.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connectionId}/queues");
            var queueId = queues[0].Id;

            var paged = await _server1.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{queueId}/messages?pageSize=100");
            Assert.HasCount(3, paged.Items);
        }

        [TestMethod]
        public async Task Source2_Messages_MatchSendCount()
        {
            var connections = await _server2.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var connectionId = connections[0].Id;

            var queues = await _server2.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connectionId}/queues");
            var queueId = queues[0].Id;

            var paged = await _server2.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{queueId}/messages?pageSize=100");
            Assert.HasCount(2, paged.Items);
        }

        [TestMethod]
        public async Task WriteOperation_RoutesToCorrectSource()
        {
            // Discover server1's queue and a message to delete
            var connections1 = await _server1.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var connectionId1 = connections1[0].Id;

            var queues1 = await _server1.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connectionId1}/queues");
            var queueId1 = queues1[0].Id;

            var paged1 = await _server1.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{queueId1}/messages?pageSize=1");
            var messageId = paged1.Items[0].QueueId;

            // Delete one message from server1
            var deleteResponse = await _server1.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{queueId1}/messages/{messageId}");
            Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Server1 should now have 2 messages
            var countResponse1 = await _server1.Client.GetAsync(
                $"api/v1/dashboard/queues/{queueId1}/messages/count");
            var count1 = await countResponse1.Content.ReadFromJsonAsync<long>();
            Assert.AreEqual(2, count1);

            // Server2 should still have 2 messages (unaffected)
            var connections2 = await _server2.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var connectionId2 = connections2[0].Id;

            var queues2 = await _server2.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connectionId2}/queues");
            var queueId2 = queues2[0].Id;

            var countResponse2 = await _server2.Client.GetAsync(
                $"api/v1/dashboard/queues/{queueId2}/messages/count");
            var count2 = await countResponse2.Content.ReadFromJsonAsync<long>();
            Assert.AreEqual(2, count2);
        }

        [TestMethod]
        public async Task Health_BothServersRespond()
        {
            var response1 = await _server1.Client.GetAsync("api/v1/dashboard/health");
            var response2 = await _server2.Client.GetAsync("api/v1/dashboard/health");

            Assert.AreEqual(HttpStatusCode.OK, response1.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, response2.StatusCode);
        }
    }
}
