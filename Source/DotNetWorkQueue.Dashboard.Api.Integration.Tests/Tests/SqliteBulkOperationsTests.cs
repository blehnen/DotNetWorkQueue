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
using DotNetWorkQueue.Transport.SQLite.Basic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    /// <summary>
    /// Tests for bulk operations: reset-all stale, error retries endpoint, and error requeue verification.
    /// </summary>
    [TestClass]
    public class SqliteBulkOperationsTests
    {
        // === Reset All Stale ===

        [TestMethod]
        public async Task ResetAllStale_WhenEmpty_ReturnsZero()
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

            // No stale messages -- all are Waiting
            var response = await server.Client.PostAsync(
                $"api/v1/dashboard/queues/{queueId}/messages/reset-all", null);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<BulkActionResponse>();
            result.Count.Should().Be(0);
        }

        [TestMethod]
        public async Task ResetAllStale_WithProcessingMessages_ResetsAll()
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

            // Create processing messages via blocking consumer with short heartbeat
            using var consumerHelper = new ConsumerStateHelper<SqLiteMessageQueueInit>();
            consumerHelper.StartBlockingConsumerShortHeartBeat(fixture.QueueConnection, fixture.Scope);
            await DashboardPollingHelper.WaitForStatusAsync(server.Client, queueId,
                s => s.Processing >= 1);

            // Reset all stale (resets all Processing messages)
            var response = await server.Client.PostAsync(
                $"api/v1/dashboard/queues/{queueId}/messages/reset-all", null);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<BulkActionResponse>();
            result.Count.Should().BeGreaterThanOrEqualTo(1);

            // Verify waiting count increased
            var status = await server.Client.GetFromJsonAsync<QueueStatusResponse>(
                $"api/v1/dashboard/queues/{queueId}/status");
            status.Waiting.Should().BeGreaterThanOrEqualTo(1);
        }

        // === Error Retries ===

        [TestMethod]
        public async Task ErrorRetries_WhenNoErrors_ReturnsEmpty()
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

            // Get a message ID
            var paged = await server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{queueId}/messages?pageSize=1");
            var messageId = paged.Items[0].QueueId;

            // No retries for this message
            var retries = await server.Client.GetFromJsonAsync<List<ErrorRetryResponse>>(
                $"api/v1/dashboard/queues/{queueId}/messages/{messageId}/retries");
            retries.Should().BeEmpty();
        }

        [TestMethod]
        public async Task ErrorRetries_AfterErrors_HasRecords()
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

            // Create errors via consumer
            var errorCountdown = new CountdownEvent(1);
            using var consumerHelper = new ConsumerStateHelper<SqLiteMessageQueueInit>();
            consumerHelper.StartErrorConsumer(fixture.QueueConnection, fixture.Scope, errorCountdown);
            errorCountdown.Wait(TimeSpan.FromSeconds(30));
            await DashboardPollingHelper.WaitForErrorsAsync(server.Client, queueId, 1);

            // Get the error message ID
            var errors = await server.Client.GetFromJsonAsync<PagedResponse<ErrorMessageResponse>>(
                $"api/v1/dashboard/queues/{queueId}/errors");
            errors.Items.Should().NotBeEmpty();
            var messageId = errors.Items[0].QueueId;

            // Check retries for this message
            var retries = await server.Client.GetFromJsonAsync<List<ErrorRetryResponse>>(
                $"api/v1/dashboard/queues/{queueId}/messages/{messageId}/retries");
            retries.Should().NotBeEmpty();
            retries[0].ExceptionType.Should().NotBeNullOrEmpty();
            retries[0].RetryCount.Should().BeGreaterThanOrEqualTo(1);
        }

        // === Requeue single error then verify status returns to waiting ===

        [TestMethod]
        public async Task RequeueError_RestoresMessageToWaiting()
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

            // Create error
            var errorCountdown = new CountdownEvent(1);
            using var consumerHelper = new ConsumerStateHelper<SqLiteMessageQueueInit>();
            consumerHelper.StartErrorConsumer(fixture.QueueConnection, fixture.Scope, errorCountdown);
            errorCountdown.Wait(TimeSpan.FromSeconds(30));
            await DashboardPollingHelper.WaitForErrorsAsync(server.Client, queueId, 1);

            // Get error message
            var errors = await server.Client.GetFromJsonAsync<PagedResponse<ErrorMessageResponse>>(
                $"api/v1/dashboard/queues/{queueId}/errors");
            var messageId = errors.Items[0].QueueId;

            // Requeue it
            var response = await server.Client.PostAsync(
                $"api/v1/dashboard/queues/{queueId}/messages/{messageId}/requeue", null);
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify status shows it back in Waiting
            var status = await server.Client.GetFromJsonAsync<QueueStatusResponse>(
                $"api/v1/dashboard/queues/{queueId}/status");
            status.Waiting.Should().BeGreaterThanOrEqualTo(1);

            // Verify error count is reduced
            var errorsAfter = await server.Client.GetFromJsonAsync<PagedResponse<ErrorMessageResponse>>(
                $"api/v1/dashboard/queues/{queueId}/errors");
            errorsAfter.Items.Count.Should().BeLessThan(errors.Items.Count);
        }

        // === Reset stale (single message) already tested in SqliteStaleTests,
        //     but verify the reset-all variant works with active processing messages ===

        [TestMethod]
        public async Task ResetStale_NotFound_ForNonProcessingMessage()
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

            // Get a waiting message
            var paged = await server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{queueId}/messages?pageSize=1");
            var messageId = paged.Items[0].QueueId;

            // Try to reset a Waiting message -- should return NotFound since it's not Processing
            var response = await server.Client.PostAsync(
                $"api/v1/dashboard/queues/{queueId}/messages/{messageId}/reset", null);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // === Cancel endpoint ===

        [TestMethod]
        public async Task CancelMessage_Returns409_WhenNoConsumerInProcess()
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

            var paged = await server.Client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{queueId}/messages?pageSize=1");
            var messageId = paged.Items[0].QueueId;

            // Cancel should return 409 when no consumer is running in-process
            var response = await server.Client.PostAsync(
                $"api/v1/dashboard/queues/{queueId}/messages/{messageId}/cancel", null);
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        // === Error pagination ===

        [TestMethod]
        public async Task Errors_Pagination()
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

            // Create errors
            var errorCountdown = new CountdownEvent(3);
            using var consumerHelper = new ConsumerStateHelper<SqLiteMessageQueueInit>();
            consumerHelper.StartErrorConsumer(fixture.QueueConnection, fixture.Scope, errorCountdown);
            errorCountdown.Wait(TimeSpan.FromSeconds(30));
            await DashboardPollingHelper.WaitForErrorsAsync(server.Client, queueId, 3);

            // First page
            var page0 = await server.Client.GetFromJsonAsync<PagedResponse<ErrorMessageResponse>>(
                $"api/v1/dashboard/queues/{queueId}/errors?pageIndex=0&pageSize=2");
            page0.Items.Should().HaveCount(2);
            page0.TotalCount.Should().BeGreaterThanOrEqualTo(3);

            // Second page
            var page1 = await server.Client.GetFromJsonAsync<PagedResponse<ErrorMessageResponse>>(
                $"api/v1/dashboard/queues/{queueId}/errors?pageIndex=1&pageSize=2");
            page1.Items.Should().NotBeEmpty();
            page1.Items.Count.Should().BeGreaterThanOrEqualTo(1);

            // Error detail fields
            page0.Items[0].LastException.Should().NotBeNullOrEmpty();
            page0.Items[0].QueueId.Should().NotBeNullOrEmpty();
        }
    }
}
