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
using DotNetWorkQueue.Transport.SQLite.Basic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    /// <summary>
    /// Tests for queue configuration, status, listing, and feature endpoints using the SQLite transport.
    /// </summary>
    [TestClass]
    public class SqliteQueueConfigTests
    {
        // === Configuration endpoint returns transport options JSON ===

        [TestMethod]
        public async Task Configuration_ReturnsNonEmptyJson()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.CreateSqliteInMemory(queueName);

            using var fixture = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                    options.Options.EnableHeartBeat = true;
                    options.Options.EnablePriority = true;
                });

            fixture.SendMessages<FakeMessage>(1);

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            var queueId = queues[0].Id;

            var config = await server.Client.GetFromJsonAsync<ConfigurationResponse>(
                $"api/v1/dashboard/queues/{queueId}/configuration");
            config.Should().NotBeNull();
            config!.ConfigurationJson.Should().NotBeNullOrEmpty();
            // The configuration JSON should contain transport-specific settings
            config.ConfigurationJson.Should().Contain("EnableStatus");
        }

        // === Status endpoint returns correct counts ===

        [TestMethod]
        public async Task Status_ReturnsCorrectCounts()
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

            fixture.SendMessages<FakeMessage>(7);

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            var queueId = queues[0].Id;

            var status = await server.Client.GetFromJsonAsync<QueueStatusResponse>(
                $"api/v1/dashboard/queues/{queueId}/status");
            status.Should().NotBeNull();
            status!.Waiting.Should().Be(7);
            status.Processing.Should().Be(0);
            status.Error.Should().Be(0);
            status.Total.Should().Be(7);
        }

        // === Features reflect transport options ===

        [TestMethod]
        public async Task Features_ReflectEnabledOptions()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.CreateSqliteInMemory(queueName);

            using var fixture = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                    options.Options.EnableHeartBeat = true;
                    options.Options.EnablePriority = true;
                    options.Options.EnableDelayedProcessing = true;
                    options.Options.EnableMessageExpiration = true;
                    options.Options.EnableRoute = true;
                });

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            var queueId = queues[0].Id;

            var features = await server.Client.GetFromJsonAsync<QueueFeaturesResponse>(
                $"api/v1/dashboard/queues/{queueId}/features");
            features.Should().NotBeNull();
            features!.EnableStatus.Should().BeTrue();
            features.EnableStatusTable.Should().BeTrue();
            features.EnableHeartBeat.Should().BeTrue();
            features.EnablePriority.Should().BeTrue();
            features.EnableDelayedProcessing.Should().BeTrue();
            features.EnableMessageExpiration.Should().BeTrue();
            features.EnableRoute.Should().BeTrue();
        }

        [TestMethod]
        public async Task Features_ReflectDisabledOptions()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.CreateSqliteInMemory(queueName);

            using var fixture = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                    // Leave other options disabled (default false)
                });

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            var queueId = queues[0].Id;

            var features = await server.Client.GetFromJsonAsync<QueueFeaturesResponse>(
                $"api/v1/dashboard/queues/{queueId}/features");
            features.Should().NotBeNull();
            features!.EnableStatus.Should().BeTrue();
            features.EnableStatusTable.Should().BeTrue();
            // SQLite defaults EnableHeartBeat to true even when not explicitly set
            features.EnableHeartBeat.Should().BeTrue();
            features.EnablePriority.Should().BeFalse();
            features.EnableDelayedProcessing.Should().BeFalse();
            features.EnableMessageExpiration.Should().BeFalse();
            features.EnableRoute.Should().BeFalse();
        }

        // === List all queues ===

        [TestMethod]
        public async Task ListQueues_ReturnsAll()
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

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            connections.Should().HaveCount(1);
            connections![0].QueueCount.Should().Be(1);

            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            queues.Should().HaveCount(1);
            queues![0].QueueName.Should().Be(queueName);
            queues[0].Id.Should().NotBeEmpty();
        }

        // === Multiple queues on same connection ===

        [TestMethod]
        public async Task MultipleQueues_AllAppear_WithCorrectCounts()
        {
            var queueName1 = QueueNameGenerator.Create();
            var queueName2 = QueueNameGenerator.Create();
            var connStr1 = ConnectionStrings.CreateSqliteInMemory(queueName1);
            var connStr2 = ConnectionStrings.CreateSqliteInMemory(queueName2);

            using var fixture1 = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName1, connStr1,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                });

            using var fixture2 = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName2, connStr2,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                });

            fixture1.SendMessages<FakeMessage>(4);
            fixture2.SendMessages<FakeMessage>(6);

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<SqLiteMessageQueueInit>(connStr1,
                    conn => conn.AddQueue(queueName1));
                options.AddConnection<SqLiteMessageQueueInit>(connStr2,
                    conn => conn.AddQueue(queueName2));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            connections.Should().HaveCount(2);

            // Find queue IDs across connections
            Guid queueId1 = Guid.Empty, queueId2 = Guid.Empty;
            foreach (var conn in connections)
            {
                var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                    $"api/v1/dashboard/connections/{conn.Id}/queues");
                foreach (var q in queues)
                {
                    if (q.QueueName == queueName1) queueId1 = q.Id;
                    if (q.QueueName == queueName2) queueId2 = q.Id;
                }
            }

            queueId1.Should().NotBeEmpty();
            queueId2.Should().NotBeEmpty();

            // Verify each queue has the correct message count
            var status1 = await server.Client.GetFromJsonAsync<QueueStatusResponse>(
                $"api/v1/dashboard/queues/{queueId1}/status");
            status1!.Waiting.Should().Be(4);
            status1.Total.Should().Be(4);

            var status2 = await server.Client.GetFromJsonAsync<QueueStatusResponse>(
                $"api/v1/dashboard/queues/{queueId2}/status");
            status2!.Waiting.Should().Be(6);
            status2.Total.Should().Be(6);
        }

        // === Jobs endpoint returns empty for queues without jobs ===

        [TestMethod]
        public async Task Jobs_ReturnsEmpty_WhenNoJobsScheduled()
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

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");

            var jobs = await server.Client.GetFromJsonAsync<List<JobResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/jobs");
            jobs.Should().BeEmpty();
        }

        // === Settings endpoint returns expected values ===

        [TestMethod]
        public async Task Settings_ReturnsExpectedDefaults()
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
            settings!.ReadOnly.Should().BeFalse();
        }

        // === Stale messages returns empty when no stale messages ===

        [TestMethod]
        public async Task StaleMessages_ReturnsEmpty_WhenNoneStale()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.CreateSqliteInMemory(queueName);

            using var fixture = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                    options.Options.EnableHeartBeat = true;
                });

            fixture.SendMessages<FakeMessage>(3);

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            var queueId = queues[0].Id;

            var paged = await server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{queueId}/messages/stale");
            paged.Should().NotBeNull();
            paged!.Items.Should().BeEmpty();
        }

        // === Errors returns empty when no errors ===

        [TestMethod]
        public async Task Errors_ReturnsEmpty_WhenNoErrors()
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

            fixture.SendMessages<FakeMessage>(2);

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            var queueId = queues[0].Id;

            var paged = await server.Client.GetFromJsonAsync<PagedResponse<ErrorMessageResponse>>(
                $"api/v1/dashboard/queues/{queueId}/errors");
            paged.Should().NotBeNull();
            paged!.Items.Should().BeEmpty();
            paged.TotalCount.Should().Be(0);
        }

        // === Message count with valid status filters ===

        [TestMethod]
        public async Task MessageCount_WithValidStatusFilters()
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

            fixture.SendMessages<FakeMessage>(5);

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            var queueId = queues[0].Id;

            // Status 0 = Waiting
            var waitingResponse = await server.Client.GetAsync(
                $"api/v1/dashboard/queues/{queueId}/messages/count?status=0");
            waitingResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var waitingCount = await waitingResponse.Content.ReadFromJsonAsync<long>();
            waitingCount.Should().Be(5);

            // Status 1 = Processing (should be 0)
            var processingResponse = await server.Client.GetAsync(
                $"api/v1/dashboard/queues/{queueId}/messages/count?status=1");
            processingResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var processingCount = await processingResponse.Content.ReadFromJsonAsync<long>();
            processingCount.Should().Be(0);

            // Status 2 = Error (should be 0)
            var errorResponse = await server.Client.GetAsync(
                $"api/v1/dashboard/queues/{queueId}/messages/count?status=2");
            errorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var errorCount = await errorResponse.Content.ReadFromJsonAsync<long>();
            errorCount.Should().Be(0);

            // No filter = total
            var totalResponse = await server.Client.GetAsync(
                $"api/v1/dashboard/queues/{queueId}/messages/count");
            totalResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var totalCount = await totalResponse.Content.ReadFromJsonAsync<long>();
            totalCount.Should().Be(5);
        }

        // === Delete all errors when empty returns zero ===

        [TestMethod]
        public async Task DeleteAllErrors_WhenEmpty_ReturnsZero()
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<DeleteAllResponse>();
            result!.Deleted.Should().Be(0);
        }

        // === Error retries for nonexistent message returns empty ===

        [TestMethod]
        public async Task ErrorRetries_NonexistentMessage_ReturnsEmpty()
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
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            var queueId = queues[0].Id;

            var retries = await server.Client.GetFromJsonAsync<List<ErrorRetryResponse>>(
                $"api/v1/dashboard/queues/{queueId}/messages/99999999/retries");
            retries.Should().BeEmpty();
        }
    }
}
