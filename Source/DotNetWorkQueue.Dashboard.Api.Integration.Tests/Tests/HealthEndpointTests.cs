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
using System.Net;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers;
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    [TestClass]
    public class HealthEndpointTests
    {
        private DashboardTestServer _server;
        private TransportFixture<MemoryDashboardInit, MessageQueueCreation> _fixture;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.Memory;

            _fixture = new TransportFixture<MemoryDashboardInit, MessageQueueCreation>(
                queueName, connStr);

            _server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<MemoryDashboardInit>(connStr,
                    serviceRegister => serviceRegister.RegisterNonScopedSingleton(_fixture.Scope),
                    conn => conn.AddQueue(queueName));
            });
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (_server != null) await _server.DisposeAsync();
            _fixture?.Dispose();
        }

        [TestMethod]
        public async Task Health_ReturnsOk()
        {
            var response = await _server.Client.GetAsync("api/v1/dashboard/health");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task Health_ResponseContainsHealthyStatus()
        {
            var response = await _server.Client.GetAsync("api/v1/dashboard/health");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(body));
            StringAssert.Contains(body, "Healthy");
        }
    }
}
