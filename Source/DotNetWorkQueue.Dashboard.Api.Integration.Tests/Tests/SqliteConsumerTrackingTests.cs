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
using DotNetWorkQueue.Transport.SQLite.Basic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    /// <summary>
    /// Tests for consumer registration, heartbeat, listing, unregister, and pruning lifecycle
    /// using the SQLite transport.
    /// </summary>
    [TestClass]
    public class SqliteConsumerTrackingTests
    {
        // === Registration with SQLite transport ===

        [TestMethod]
        public async Task Register_WithSqliteQueue_Returns201_And_MatchesQueue()
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

            // Register a consumer against the SQLite queue
            var body = new { QueueName = queueName, MachineName = "SQLITEHOST", ProcessId = 5000, FriendlyName = "SqliteWorker" };
            var response = await server.Client.PostAsJsonAsync("api/v1/dashboard/consumers/register", body);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var registration = await response.Content.ReadFromJsonAsync<ConsumerRegistrationResponse>();
            registration.Should().NotBeNull();
            registration!.ConsumerId.Should().NotBeEmpty();
            registration.HeartbeatIntervalSeconds.Should().BeGreaterThan(0);

            // Verify consumer appears in list and is matched to the queue
            var consumers = await server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");
            consumers.Should().HaveCount(1);
            consumers![0].MatchedQueueId.Should().Be(queueId);
            consumers[0].MachineName.Should().Be("SQLITEHOST");
            consumers[0].QueueName.Should().Be(queueName);
        }

        // === Heartbeat updates timestamp ===

        [TestMethod]
        public async Task Heartbeat_UpdatesTimestamp_ForSqliteConsumer()
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

            // Register
            var regBody = new { QueueName = queueName, MachineName = "HOST1", ProcessId = 100 };
            var regResponse = await server.Client.PostAsJsonAsync("api/v1/dashboard/consumers/register", regBody);
            var registration = await regResponse.Content.ReadFromJsonAsync<ConsumerRegistrationResponse>();

            // Get initial heartbeat time
            var consumersBefore = await server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");
            var initialHeartbeat = consumersBefore![0].LastHeartbeat;

            // Small delay to ensure time advances
            await Task.Delay(50);

            // Send heartbeat with metrics
            var hbBody = new
            {
                ConsumerId = registration!.ConsumerId,
                MessagesProcessed = 50L,
                MessagesErrored = 2L,
                MessagesRolledBack = 1L,
                PoisonMessages = 0L
            };
            var hbResponse = await server.Client.PostAsJsonAsync("api/v1/dashboard/consumers/heartbeat", hbBody);
            hbResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify timestamp updated and metrics present
            var consumersAfter = await server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");
            consumersAfter![0].LastHeartbeat.Should().BeOnOrAfter(initialHeartbeat);
            consumersAfter[0].MessagesProcessed.Should().Be(50);
            consumersAfter[0].MessagesErrored.Should().Be(2);
            consumersAfter[0].MessagesRolledBack.Should().Be(1);
            consumersAfter[0].PoisonMessages.Should().Be(0);
        }

        // === List consumers filtered by queue ===

        [TestMethod]
        public async Task GetConsumers_FilteredByQueueId_ReturnsOnlyMatched()
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
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            var queueId = queues[0].Id;

            // Register two consumers against this queue and one against a nonexistent queue
            await server.Client.PostAsJsonAsync("api/v1/dashboard/consumers/register",
                new { QueueName = queueName, MachineName = "M1", ProcessId = 1000 });
            await server.Client.PostAsJsonAsync("api/v1/dashboard/consumers/register",
                new { QueueName = queueName, MachineName = "M2", ProcessId = 2000 });
            await server.Client.PostAsJsonAsync("api/v1/dashboard/consumers/register",
                new { QueueName = "nonexistent_queue", MachineName = "M3", ProcessId = 3000 });

            // Filter by queueId should return only the 2 matched
            var filtered = await server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                $"api/v1/dashboard/consumers?queueId={queueId}");
            filtered.Should().HaveCount(2);

            // All should return all 3
            var all = await server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");
            all.Should().HaveCount(3);
        }

        // === Unregister removes consumer ===

        [TestMethod]
        public async Task Unregister_RemovesConsumer_FromSqliteQueue()
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

            // Register
            var regResponse = await server.Client.PostAsJsonAsync("api/v1/dashboard/consumers/register",
                new { QueueName = queueName, MachineName = "HOST", ProcessId = 999 });
            var registration = await regResponse.Content.ReadFromJsonAsync<ConsumerRegistrationResponse>();

            // Verify registered
            var consumers = await server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");
            consumers.Should().HaveCount(1);

            // Unregister
            var deleteResponse = await server.Client.DeleteAsync(
                $"api/v1/dashboard/consumers/{registration!.ConsumerId}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify gone
            consumers = await server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");
            consumers.Should().BeEmpty();
        }

        // === Consumer pruning service prunes stale consumers ===

        [TestMethod]
        public async Task PruningService_RemovesStaleConsumers()
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

            // Configure very short pruning intervals so the background service prunes quickly
            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.ConsumerHeartbeatIntervalSeconds = 1;
                options.ConsumerStaleThresholdSeconds = 2;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            // Register a consumer but do NOT send heartbeats
            var regResponse = await server.Client.PostAsJsonAsync("api/v1/dashboard/consumers/register",
                new { QueueName = queueName, MachineName = "STALEHOST", ProcessId = 7777 });
            regResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // Verify it exists
            var consumers = await server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");
            consumers.Should().HaveCount(1);

            // Wait for the pruning service to remove it (stale threshold = 2s, prune interval = 1s)
            // Poll up to 10 seconds to account for timing
            var deadline = DateTime.UtcNow.AddSeconds(10);
            var pruned = false;
            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(500);
                consumers = await server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                    "api/v1/dashboard/consumers");
                if (consumers!.Count == 0)
                {
                    pruned = true;
                    break;
                }
            }

            pruned.Should().BeTrue("Consumer should have been pruned by the ConsumerPruningService");
        }

        // === Consumer tracking disabled returns appropriate responses ===

        [TestMethod]
        public async Task ConsumerTracking_Disabled_ReturnsNotFound()
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
                options.EnableConsumerTracking = false;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            // Register should return 404 when tracking is disabled
            var regResponse = await server.Client.PostAsJsonAsync("api/v1/dashboard/consumers/register",
                new { QueueName = queueName, MachineName = "HOST", ProcessId = 100 });
            regResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

            // Heartbeat should return 404
            var hbResponse = await server.Client.PostAsJsonAsync("api/v1/dashboard/consumers/heartbeat",
                new { ConsumerId = Guid.NewGuid(), MessagesProcessed = 0L, MessagesErrored = 0L, MessagesRolledBack = 0L, PoisonMessages = 0L });
            hbResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

            // Unregister should return 404
            var deleteResponse = await server.Client.DeleteAsync(
                $"api/v1/dashboard/consumers/{Guid.NewGuid()}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

            // GET consumers returns empty list (not 404)
            var consumers = await server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");
            consumers.Should().BeEmpty();

            // GET counts returns empty dictionary
            var counts = await server.Client.GetFromJsonAsync<Dictionary<Guid, int>>(
                "api/v1/dashboard/consumers/count");
            counts.Should().BeEmpty();
        }

        // === Consumer counts per queue ===

        [TestMethod]
        public async Task ConsumerCounts_ReflectRegistrations()
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
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            var queueId = queues[0].Id;

            // No consumers initially
            var counts = await server.Client.GetFromJsonAsync<Dictionary<Guid, int>>(
                "api/v1/dashboard/consumers/count");
            counts.Should().BeEmpty();

            // Register two
            await server.Client.PostAsJsonAsync("api/v1/dashboard/consumers/register",
                new { QueueName = queueName, MachineName = "M1", ProcessId = 1 });
            await server.Client.PostAsJsonAsync("api/v1/dashboard/consumers/register",
                new { QueueName = queueName, MachineName = "M2", ProcessId = 2 });

            counts = await server.Client.GetFromJsonAsync<Dictionary<Guid, int>>(
                "api/v1/dashboard/consumers/count");
            counts.Should().ContainKey(queueId);
            counts![queueId].Should().Be(2);

            // Unmatched queue consumer should not appear in counts
            await server.Client.PostAsJsonAsync("api/v1/dashboard/consumers/register",
                new { QueueName = "unknown_queue", MachineName = "M3", ProcessId = 3 });

            counts = await server.Client.GetFromJsonAsync<Dictionary<Guid, int>>(
                "api/v1/dashboard/consumers/count");
            counts![queueId].Should().Be(2); // still 2, unmatched not counted
        }
    }
}
