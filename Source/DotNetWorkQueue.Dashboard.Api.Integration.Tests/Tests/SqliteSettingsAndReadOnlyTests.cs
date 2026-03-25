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
using DotNetWorkQueue.Transport.SQLite.Basic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    [TestClass]
    public class SqliteSettingsAndReadOnlyTests
    {
        // === Settings endpoint ===

        [TestMethod]
        public async Task Settings_ReturnsReadOnlyFalse_ByDefault()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.CreateSqliteInMemory(queueName);

            using var fixture = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                });

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var settings = await server.Client.GetFromJsonAsync<DashboardSettingsResponse>(
                "api/v1/dashboard/settings");
            settings.Should().NotBeNull();
            settings.ReadOnly.Should().BeFalse();
        }

        [TestMethod]
        public async Task Settings_ReturnsReadOnlyTrue_WhenConfigured()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.CreateSqliteInMemory(queueName);

            using var fixture = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                });

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.ReadOnly = true;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var settings = await server.Client.GetFromJsonAsync<DashboardSettingsResponse>(
                "api/v1/dashboard/settings");
            settings.Should().NotBeNull();
            settings.ReadOnly.Should().BeTrue();
        }

        // === ReadOnly mode blocks write operations ===

        [TestMethod]
        public async Task ReadOnly_AllowsGetRequests()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.CreateSqliteInMemory(queueName);

            using var fixture = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                });

            fixture.SendMessages<FakeMessage>(3);

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.ReadOnly = true;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            // GET requests should still work
            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            connections.Should().HaveCount(1);

            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            queues.Should().HaveCount(1);

            var status = await server.Client.GetFromJsonAsync<QueueStatusResponse>(
                $"api/v1/dashboard/queues/{queues[0].Id}/status");
            status.Total.Should().Be(3);
        }

        [TestMethod]
        public async Task ReadOnly_BlocksDeleteMessage()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.CreateSqliteInMemory(queueName);

            using var fixture = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                });

            fixture.SendMessages<FakeMessage>(1);

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.ReadOnly = true;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            var queueId = queues[0].Id;

            var paged = await server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{queueId}/messages?pageSize=1");
            var messageId = paged.Items[0].QueueId;

            var response = await server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{queueId}/messages/{messageId}");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [TestMethod]
        public async Task ReadOnly_BlocksRequeueError()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.CreateSqliteInMemory(queueName);

            using var fixture = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                });

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.ReadOnly = true;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            var queueId = queues[0].Id;

            var response = await server.Client.PostAsync(
                $"api/v1/dashboard/queues/{queueId}/messages/12345/requeue", null);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [TestMethod]
        public async Task ReadOnly_BlocksDeleteAllErrors()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.CreateSqliteInMemory(queueName);

            using var fixture = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                });

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.ReadOnly = true;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            var queueId = queues[0].Id;

            var response = await server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{queueId}/errors");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [TestMethod]
        public async Task ReadOnly_BlocksEditBody()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.CreateSqliteInMemory(queueName);

            using var fixture = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                });

            fixture.SendMessages<FakeMessage>(1);

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.ReadOnly = true;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            var queueId = queues[0].Id;

            var paged = await server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{queueId}/messages?pageSize=1");
            var messageId = paged.Items[0].QueueId;

            var content = new StringContent(
                "{\"Body\":\"{\\\"Message\\\":\\\"edited\\\",\\\"Value\\\":99}\"}",
                Encoding.UTF8, "application/json");
            var response = await server.Client.PutAsync(
                $"api/v1/dashboard/queues/{queueId}/messages/{messageId}/body", content);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [TestMethod]
        public async Task ReadOnly_BlocksResetStale()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.CreateSqliteInMemory(queueName);

            using var fixture = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                });

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.ReadOnly = true;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            var queueId = queues[0].Id;

            var response = await server.Client.PostAsync(
                $"api/v1/dashboard/queues/{queueId}/messages/12345/reset", null);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [TestMethod]
        public async Task ReadOnly_BlocksPurgeHistory()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.CreateSqliteInMemory(queueName);

            using var fixture = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                });

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.ReadOnly = true;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            var queueId = queues[0].Id;

            var response = await server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{queueId}/history");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}
