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
using System.IO;
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
    public class AssemblyPathTests
    {
        private DashboardTestServer _server;
        private TransportFixture<MemoryDashboardInit, MessageQueueCreation> _fixture;
        private Guid _queueId;
        private string _pluginDir;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            _pluginDir = Path.Combine(Path.GetTempPath(), "dnwq-test-plugins-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_pluginDir);

            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.Memory;

            _fixture = new TransportFixture<MemoryDashboardInit, MessageQueueCreation>(
                queueName, connStr);

            _fixture.SendMessages<FakeMessage>(3);

            _server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AssemblyPaths = new[] { _pluginDir };
                options.AddConnection<MemoryDashboardInit>(connStr,
                    serviceRegister => serviceRegister.RegisterNonScopedSingleton(_fixture.Scope),
                    conn => conn.AddQueue(queueName));
            });

            var connections = await _server.Client.GetFromJsonAsync<ConnectionResponse[]>(
                "api/v1/dashboard/connections");
            var queues = await _server.Client.GetFromJsonAsync<QueueInfoResponse[]>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            _queueId = queues[0].Id;
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (_server != null) await _server.DisposeAsync();
            _fixture?.Dispose();
            if (Directory.Exists(_pluginDir))
                Directory.Delete(_pluginDir, true);
        }

        [TestMethod]
        public async Task MessageBody_ResolvedWithAssemblyPaths()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?pageSize=1");
            var messageId = paged.Items[0].QueueId;

            var body = await _server.Client.GetFromJsonAsync<MessageBodyResponse>(
                $"api/v1/dashboard/queues/{_queueId}/messages/{messageId}/body");

            body.Body.Should().NotBeNullOrEmpty();
            body.TypeName.Should().Contain("FakeMessage");
        }

        [TestMethod]
        public async Task MessageBody_EmptyPluginDir_StillWorks()
        {
            // Plugin dir exists but has no DLLs — body should still resolve
            // because FakeMessage is already in the AppDomain
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?pageSize=1");
            var messageId = paged.Items[0].QueueId;

            var body = await _server.Client.GetFromJsonAsync<MessageBodyResponse>(
                $"api/v1/dashboard/queues/{_queueId}/messages/{messageId}/body");

            body.Body.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task MessageBody_NonexistentPluginDir_DoesNotCrash()
        {
            // Delete the plugin dir before querying — should not throw
            Directory.Delete(_pluginDir, true);

            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/messages?pageSize=1");
            var messageId = paged.Items[0].QueueId;

            var body = await _server.Client.GetFromJsonAsync<MessageBodyResponse>(
                $"api/v1/dashboard/queues/{_queueId}/messages/{messageId}/body");

            body.Body.Should().NotBeNullOrEmpty();
        }
    }
}
