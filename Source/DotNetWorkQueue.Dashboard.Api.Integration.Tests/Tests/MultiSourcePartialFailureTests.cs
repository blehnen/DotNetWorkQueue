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
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers;
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    [TestClass]
    public class MultiSourcePartialFailureTests
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
            _fixture2.SendMessages<FakeMessage>(3);

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
            try
            {
                if (_server1 != null) await _server1.DisposeAsync();
            }
            catch
            {
                // best-effort cleanup
            }

            try
            {
                if (_server2 != null) await _server2.DisposeAsync();
            }
            catch
            {
                // best-effort cleanup
            }

            try { _fixture1?.Dispose(); } catch { /* best-effort */ }
            try { _fixture2?.Dispose(); } catch { /* best-effort */ }
        }

        [TestMethod]
        public async Task AfterDispose_DisposedServerReturnsError()
        {
            // Verify server2 is healthy first
            var connections = await _server2.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            Assert.AreEqual(1, connections.Count);

            // Dispose server2
            await _server2.DisposeAsync();

            // Attempt to call disposed server - should throw
            var exceptionThrown = false;
            try
            {
                await _server2.Client.GetAsync("api/v1/dashboard/connections");
            }
            catch (Exception ex) when (ex is HttpRequestException or ObjectDisposedException or InvalidOperationException)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown);
            _server2 = null; // prevent double-dispose in cleanup
        }

        [TestMethod]
        public async Task AfterDispose_OtherServerStillWorks()
        {
            await _server2.DisposeAsync();
            _server2 = null;

            var connections = await _server1.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            Assert.AreEqual(1, connections.Count);
            Assert.AreEqual(1, connections[0].QueueCount);
        }

        [TestMethod]
        public async Task AfterDispose_OtherServerMessagesIntact()
        {
            await _server2.DisposeAsync();
            _server2 = null;

            var connections = await _server1.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var connectionId = connections[0].Id;

            var queues = await _server1.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connectionId}/queues");
            var queueId = queues[0].Id;

            var paged = await _server1.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{queueId}/messages?pageSize=100");
            Assert.AreEqual(3, paged.Items.Count);
        }

        [TestMethod]
        public async Task AfterDispose_OtherServerHealthStillOk()
        {
            await _server2.DisposeAsync();
            _server2 = null;

            var response = await _server1.Client.GetAsync("api/v1/dashboard/health");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
