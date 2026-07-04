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
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers;
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    [TestClass]
    public class LiteDbErrorTests
    {
        private DashboardTestServer _server;
        private TransportFixture<LiteDbMessageQueueInit, LiteDbMessageQueueCreation> _fixture;
        private ConsumerStateHelper<LiteDbMessageQueueInit> _consumerHelper;
        private Guid _queueId;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.LiteDbMemory;

            _fixture = new TransportFixture<LiteDbMessageQueueInit, LiteDbMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatusTable = true;
                });

            _fixture.SendMessages<FakeMessage>(2);

            _server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<LiteDbMessageQueueInit>(connStr,
                    serviceRegister => serviceRegister.RegisterNonScopedSingleton(_fixture.Scope),
                    conn => conn.AddQueue(queueName));
            });

            var connections = await _server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await _server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            _queueId = queues[0].Id;

            var errorCountdown = new CountdownEvent(2);
            _consumerHelper = new ConsumerStateHelper<LiteDbMessageQueueInit>();
            _consumerHelper.StartErrorConsumer(_fixture.QueueConnection, _fixture.Scope, errorCountdown);
            errorCountdown.Wait(TimeSpan.FromSeconds(30));

            // Poll until errors are visible via the dashboard API
            await DashboardPollingHelper.WaitForErrorsAsync(_server.Client, _queueId, 2);
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            _consumerHelper?.Dispose();
            if (_server != null) await _server.DisposeAsync();
            _fixture?.Dispose();
        }

        [TestMethod]
        public async Task Status_WithErrors()
        {
            var status = await _server.Client.GetFromJsonAsync<QueueStatusResponse>(
                $"api/v1/dashboard/queues/{_queueId}/status");
            Assert.IsTrue(status.Error > 0);
        }

        [TestMethod]
        public async Task Errors_AfterFailure()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<ErrorMessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/errors");
            Assert.IsTrue(paged.Items.Count > 0);
            Assert.IsFalse(string.IsNullOrEmpty(paged.Items[0].LastException));
        }

        [TestMethod]
        public async Task DeleteAllErrors_AfterFailure()
        {
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{_queueId}/errors");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DeleteAllResponse>();
            Assert.IsTrue(result.Deleted > 0);

            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<ErrorMessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/errors");
            Assert.AreEqual(0, paged.Items.Count);
        }

        [TestMethod]
        public async Task RequeueError_Success()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<ErrorMessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/errors");
            Assert.IsTrue(paged.Items.Count > 0);
            var messageId = paged.Items[0].QueueId;

            var response = await _server.Client.PostAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/{messageId}/requeue", null);
            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
